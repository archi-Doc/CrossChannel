// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1611

namespace CrossChannel.Generator;

public partial class CrossChannelObject
{
    private const string TaskName = "System.Threading.Tasks.Task";

    internal void GenerateBrokerClass(ScopingStringBuilder ssb)
    {
        using (ssb.ScopeBrace($"private sealed class {this.BrokerClassName} : {this.LocalName}"))
        {
            ssb.AppendLine($"private readonly Channel<{this.LocalName}> channel;");
            using (ssb.ScopeBrace($"public {this.BrokerClassName}(object channel)"))
            {
                ssb.AppendLine($"this.channel = (Channel<{this.LocalName}>)channel;");
            }

            if (this.Methods is not null)
            {
                foreach (var x in this.Methods)
                {
                    if (x.ReturnKind == ServiceMethod.MethodReturnKind.Other)
                    {
                        continue;
                    }

                    // The broker methods are deliberately not 'async': the common cases (no receiver,
                    // a single receiver) complete without allocating an async state machine.
                    using (ssb.ScopeBrace($"{x.ReturnName} {x.DeclaringName}.@{x.SimpleName}({x.GetParameterDeclarations()})"))
                    {
                        if (x.ReturnKind == ServiceMethod.MethodReturnKind.Void)
                        {
                            this.GenerateBrokerMethod_Void(ssb, x);
                        }
                        else if (x.ReturnKind == ServiceMethod.MethodReturnKind.RadioResult)
                        {
                            this.GenerateBrokerMethod_RadioResult(ssb, x);
                        }
                        else if (x.ReturnKind == ServiceMethod.MethodReturnKind.Task)
                        {
                            this.GenerateBrokerMethod_Task(ssb, x);
                        }
                        else if (x.ReturnKind == ServiceMethod.MethodReturnKind.TaskRadioResult)
                        {
                            this.GenerateBrokerMethod_TaskRadioResult(ssb, x);
                        }
                    }
                }
            }
        }
    }

    private void GenerateBrokerMethod_Void(ScopingStringBuilder ssb, ServiceMethod method)
    {// void
        this.Generate_GetList(ssb);
        ssb.AppendLine("if (countHint == 0) return;");

        using (this.Generate_ForEach(ssb))
        {
            ssb.AppendLine($"(({method.DeclaringName})instance).@{method.SimpleName}({method.GetParameterNames()});");
        }
    }

    private void GenerateBrokerMethod_RadioResult(ScopingStringBuilder ssb, ServiceMethod method)
    {// RadioResult<T>
        this.Generate_GetList(ssb);
        ssb.AppendLine("if (countHint == 0) return default;");
        ssb.AppendLine($"{method.ResultName} firstResult = default!;");
        ssb.AppendLine($"var results = System.Array.Empty<{method.ResultName}>();");
        ssb.AppendLine("var count = 0;");

        using (this.Generate_ForEach(ssb))
        {
            ssb.AppendLine($"if (!(({method.DeclaringName})instance).@{method.SimpleName}({method.GetParameterNames()}).TryGetFirst(out var r)) continue;");
            this.Generate_AddValue(ssb, method.ResultName, "results", "firstResult", "r");
        }

        // 0 receivers: empty, 1 receiver: a single result (no array is allocated).
        ssb.AppendLine("if (count == 0) return default;");
        ssb.AppendLine("else if (count == 1) return new(firstResult);");
        ssb.AppendLine("else if (count != results!.Length) System.Array.Resize(ref results, count);");
        ssb.AppendLine("return new(results!);");
    }

    private void GenerateBrokerMethod_Task(ScopingStringBuilder ssb, ServiceMethod method)
    {// Task
        var taskName = method.ReturnName; // System.Threading.Tasks.Task

        this.Generate_GetList(ssb);
        ssb.AppendLine($"if (countHint == 0) return {TaskName}.CompletedTask;");
        ssb.AppendLine($"{taskName}? firstTask = default;");
        ssb.AppendLine($"{taskName}[]? tasks = default;");
        ssb.AppendLine("var count = 0;");

        // Since this method is not 'async', a synchronous exception has to be captured
        // in the returned task in order to preserve the behavior for the callers.
        using (ssb.ScopeBrace("try"))
        {
            using (this.Generate_ForEach(ssb))
            {
                ssb.AppendLine($"var t = (({method.DeclaringName})instance).@{method.SimpleName}({method.GetParameterNames()});");
                ssb.AppendLine("if (t.IsCompletedSuccessfully) continue;");
                this.Generate_AddValue(ssb, taskName, "tasks", "firstTask", "t");
            }
        }

        using (ssb.ScopeBrace("catch (System.Exception ex)"))
        {
            ssb.AppendLine($"return {TaskName}.FromException(ex);");
        }

        // 0 receivers: a cached completed task, 1 receiver: the task is passed through as-is.
        ssb.AppendLine($"if (count == 0) return {TaskName}.CompletedTask;");
        ssb.AppendLine("else if (count == 1) return firstTask!;");
        ssb.AppendLine($"return {TaskName}.WhenAll(tasks!.AsSpan(0, count));");
    }

    private void GenerateBrokerMethod_TaskRadioResult(ScopingStringBuilder ssb, ServiceMethod method)
    {// Task<RadioResult<T>>
        var taskName = method.ReturnName; // System.Threading.Tasks.Task<CrossChannel.RadioResult<T>>
        var emptyTask = $"CrossChannel.RadioTask.GetEmptyResultTask<{method.ResultName}>()";

        this.Generate_GetList(ssb);
        ssb.AppendLine($"if (countHint == 0) return {emptyTask};");
        ssb.AppendLine($"{taskName}? firstTask = default;");
        ssb.AppendLine($"{taskName}[]? tasks = default;");
        ssb.AppendLine("var count = 0;");

        using (ssb.ScopeBrace("try"))
        {
            using (this.Generate_ForEach(ssb))
            {
                ssb.AppendLine($"var t = (({method.DeclaringName})instance).@{method.SimpleName}({method.GetParameterNames()});");
                this.Generate_AddValue(ssb, taskName, "tasks", "firstTask", "t");
            }
        }

        using (ssb.ScopeBrace("catch (System.Exception ex)"))
        {
            ssb.AppendLine($"return {TaskName}.FromException<CrossChannel.RadioResult<{method.ResultName}>>(ex);");
        }

        // 0 receivers: a cached completed task, 1 receiver: the task is passed through as-is.
        ssb.AppendLine($"if (count == 0) return {emptyTask};");
        ssb.AppendLine("else if (count == 1) return firstTask!;");
        ssb.AppendLine($"return CrossChannel.RadioTask.AggregateAsync<{method.ResultName}>({TaskName}.WhenAll(tasks!.AsSpan(0, count)));");
    }

    private void Generate_GetList(ScopingStringBuilder ssb)
        => ssb.AppendLine("var (array, countHint) = this.channel.DangerousGetLinks();");

    /// <summary>
    /// Enumerates the captured array without treating the concurrent count hint as a limit.
    /// </summary>
    private ScopingStringBuilder.IScope Generate_ForEach(ScopingStringBuilder ssb)
    {
        var scope = ssb.ScopeBrace("for (var linkIndex = 0; linkIndex < array.Length; linkIndex++)");
        ssb.AppendLine("var x = System.Threading.Volatile.Read(ref array[linkIndex]);");
        ssb.AppendLine("if (x is null || !x.IsOpen) continue;");
        ssb.AppendLine("if (!x.TryGetInstance(out var instance)) { x.Dispose(); continue; }");

        return scope;
    }

    /// <summary>
    /// Stores a value, deferring the allocation of the array until a second value arrives.
    /// </summary>
    private void Generate_AddValue(ScopingStringBuilder ssb, string elementName, string arrayName, string firstName, string value)
    {
        using (ssb.ScopeBrace("if (count == 0)"))
        {
            ssb.AppendLine($"{firstName} = {value};");
        }

        using (ssb.ScopeBrace("else"))
        {
            using (ssb.ScopeBrace($"if ({arrayName} is null || {arrayName}.Length == 0)"))
            {
                ssb.AppendLine($"{arrayName} = System.GC.AllocateUninitializedArray<{elementName}>(System.Math.Max(2, countHint));");
                ssb.AppendLine($"{arrayName}[0] = {firstName}!;");
            }

            using (ssb.ScopeBrace($"else if (count == {arrayName}.Length)"))
            {
                ssb.AppendLine($"System.Array.Resize(ref {arrayName}, System.Math.Min(array.Length, {arrayName}.Length * 2));");
            }

            ssb.AppendLine($"{arrayName}[count] = {value};");
        }

        ssb.AppendLine("count++;");
    }
}
