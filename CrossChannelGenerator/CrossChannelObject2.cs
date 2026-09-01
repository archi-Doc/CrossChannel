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
        using (ssb.ScopeBrace($"private sealed class {this.ClassName} : {this.LocalName}"))
        {
            ssb.AppendLine($"private readonly Channel<{this.LocalName}> channel;");
            using (ssb.ScopeBrace($"public {this.ClassName}(object channel)"))
            {
                ssb.AppendLine($"this.channel = (Channel<{this.LocalName}>)channel;");
            }

            if (this.Methods is not null)
            {
                foreach (var x in this.Methods)
                {
                    if (x.ReturnType == ServiceMethod.Type.Other)
                    {
                        continue;
                    }

                    // The broker methods are deliberately not 'async': the common cases (no receiver,
                    // a single receiver) complete without allocating an async state machine.
                    using (ssb.ScopeBrace($"{x.ReturnObject.FullName} {this.LocalName}.{x.SimpleName}({x.GetParameters()})"))
                    {
                        if (x.ReturnType == ServiceMethod.Type.Void)
                        {
                            this.GenerateBrokerMethod_Void(ssb, x);
                        }
                        else if (x.ReturnType == ServiceMethod.Type.RadioResult)
                        {
                            this.GenerateBrokerMethod_RadioResult(ssb, x);
                        }
                        else if (x.ReturnType == ServiceMethod.Type.Task)
                        {
                            this.GenerateBrokerMethod_Task(ssb, x);
                        }
                        else if (x.ReturnType == ServiceMethod.Type.TaskRadioResult)
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
        ssb.AppendLine("var count = 0;");

        using (this.Generate_ForEach(ssb))
        {
            ssb.AppendLine($"instance.{method.SimpleName}({method.GetParameterNames()});");
            ssb.AppendLine("if (++count >= countHint) break;");
        }
    }

    private void GenerateBrokerMethod_RadioResult(ScopingStringBuilder ssb, ServiceMethod method)
    {// RadioResult<T>
        this.Generate_GetList(ssb);
        ssb.AppendLine("if (countHint == 0) return default;");
        ssb.AppendLine($"{method.ResultName} firstResult = default!;");
        ssb.AppendLine($"{method.ResultName}[]? results = default;");
        ssb.AppendLine("var count = 0;");

        using (this.Generate_ForEach(ssb))
        {
            ssb.AppendLine($"if (!instance.{method.SimpleName}({method.GetParameterNames()}).TryGetSingleResult(out var r)) continue;");
            this.Generate_AddValue(ssb, method.ResultName, "results", "firstResult", "r");
        }

        // 0 receivers: empty, 1 receiver: a single result (no array is allocated).
        ssb.AppendLine("if (count == 0) return default;");
        ssb.AppendLine("else if (count == 1) return new(firstResult);");
        ssb.AppendLine("else if (count != countHint) System.Array.Resize(ref results, count);");
        ssb.AppendLine("return new(results!);");
    }

    private void GenerateBrokerMethod_Task(ScopingStringBuilder ssb, ServiceMethod method)
    {// Task
        var taskName = method.ReturnObject.FullName; // System.Threading.Tasks.Task

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
                ssb.AppendLine($"var t = instance.{method.SimpleName}({method.GetParameterNames()});");
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
        var taskName = method.ReturnObject.FullName; // System.Threading.Tasks.Task<CrossChannel.RadioResult<T>>
        var emptyTask = $"CrossChannel.RadioTask.Empty<{method.ResultName}>()";

        this.Generate_GetList(ssb);
        ssb.AppendLine($"if (countHint == 0) return {emptyTask};");
        ssb.AppendLine($"{taskName}? firstTask = default;");
        ssb.AppendLine($"{taskName}[]? tasks = default;");
        ssb.AppendLine("var count = 0;");

        using (ssb.ScopeBrace("try"))
        {
            using (this.Generate_ForEach(ssb))
            {
                ssb.AppendLine($"var t = instance.{method.SimpleName}({method.GetParameterNames()});");
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
        ssb.AppendLine($"return CrossChannel.RadioTask.Aggregate<{method.ResultName}>({TaskName}.WhenAll(tasks!.AsSpan(0, count)));");
    }

    private void Generate_GetList(ScopingStringBuilder ssb)
        => ssb.AppendLine("var (array, countHint) = this.channel.InternalGetList();");

    /// <summary>
    /// Enumerates the links of the channel.<br/>
    /// The loop body is expected to increment 'count' and to break once it reaches 'countHint',
    /// so that the unused part of the array is not scanned.
    /// </summary>
    private ScopingStringBuilder.IScope Generate_ForEach(ScopingStringBuilder ssb)
    {
        var scope = ssb.ScopeBrace("foreach (var x in array)");
        ssb.AppendLine("if (x is null) continue;");
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
            using (ssb.ScopeBrace($"if ({arrayName} is null)"))
            {
                ssb.AppendLine($"{arrayName} = new {elementName}[countHint];");
                ssb.AppendLine($"{arrayName}[0] = {firstName}!;");
            }

            ssb.AppendLine($"{arrayName}[count] = {value};");
        }

        ssb.AppendLine("if (++count >= countHint) break;");
    }
}
