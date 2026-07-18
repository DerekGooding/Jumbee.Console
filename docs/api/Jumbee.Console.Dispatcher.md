# <a id="Jumbee_Console_Dispatcher"></a> Class Dispatcher

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Owns a single UI thread and a serialized work queue. UI state mutation and rendering are intended to
run on this thread; other threads marshal work onto it via <xref href="Jumbee.Console.Dispatcher.Post(System.Action)" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Dispatcher.Invoke(System.Action)" data-throw-if-not-resolved="false"></xref>,
or <xref href="Jumbee.Console.Dispatcher.InvokeAsync(System.Action)" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class Dispatcher
```

#### Inheritance

object ← 
[Dispatcher](Jumbee.Console.Dispatcher.md)

## Properties

### <a id="Jumbee_Console_Dispatcher_IsRunning"></a> IsRunning

Gets a value indicating whether the UI loop is running.

```csharp
public bool IsRunning { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Dispatcher_ThreadId"></a> ThreadId

Gets the managed thread id of the UI thread, or -1 when not running.

```csharp
public int ThreadId { get; }
```

#### Property Value

 int

## Methods

### <a id="Jumbee_Console_Dispatcher_CheckAccess"></a> CheckAccess\(\)

Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when the caller is on the UI thread, or when no UI thread is running
(so headless/inline callers are treated as having access).

```csharp
public bool CheckAccess()
```

#### Returns

 bool

### <a id="Jumbee_Console_Dispatcher_Invoke_System_Action_"></a> Invoke\(Action\)

Runs an action on the UI thread, blocking until it completes. Runs inline when already on the UI
thread (or when no UI thread is running). Exceptions are rethrown on the calling thread.

```csharp
public void Invoke(Action action)
```

#### Parameters

`action` Action

### <a id="Jumbee_Console_Dispatcher_InvokeAsync_System_Action_"></a> InvokeAsync\(Action\)

Runs an action on the UI thread and returns a task that completes when it finishes.

```csharp
public Task InvokeAsync(Action action)
```

#### Parameters

`action` Action

#### Returns

 Task

### <a id="Jumbee_Console_Dispatcher_InvokeAsync__1_System_Func___0__"></a> InvokeAsync<T\>\(Func<T\>\)

Runs a function on the UI thread and returns a task with its result.

```csharp
public Task<T> InvokeAsync<T>(Func<T> func)
```

#### Parameters

`func` Func<T\>

#### Returns

 Task<T\>

#### Type Parameters

`T` 

### <a id="Jumbee_Console_Dispatcher_InvokeAsync_System_Func_System_Threading_Tasks_Task__"></a> InvokeAsync\(Func<Task\>\)

Runs an async delegate on the UI thread and returns a task that completes when the delegate's task
completes (unwrapped), so awaiting it waits for the whole operation and propagates its exceptions.

```csharp
public Task InvokeAsync(Func<Task> func)
```

#### Parameters

`func` Func<Task\>

#### Returns

 Task

### <a id="Jumbee_Console_Dispatcher_InvokeAsync__1_System_Func_System_Threading_Tasks_Task___0___"></a> InvokeAsync<T\>\(Func<Task<T\>\>\)

Runs an async function on the UI thread and returns a task with its (unwrapped) result.

```csharp
public Task<T> InvokeAsync<T>(Func<Task<T>> func)
```

#### Parameters

`func` Func<Task<T\>\>

#### Returns

 Task<T\>

#### Type Parameters

`T` 

### <a id="Jumbee_Console_Dispatcher_Post_System_Action_"></a> Post\(Action\)

Queues an action to run on the UI thread and wakes the loop.

```csharp
public void Post(Action action)
```

#### Parameters

`action` Action

### <a id="Jumbee_Console_Dispatcher_Start_System_Action_System_Int32_"></a> Start\(Action, int\)

Starts the UI thread. <code class="paramref">frame</code> runs once per frame after the queue is drained;
the loop also wakes every <code class="paramref">frameIntervalMs</code> so animations advance without input.

```csharp
public void Start(Action frame, int frameIntervalMs = 100)
```

#### Parameters

`frame` Action

`frameIntervalMs` int

### <a id="Jumbee_Console_Dispatcher_Stop"></a> Stop\(\)

Signals the UI thread to stop, waits briefly for it to exit, and clears any pending work.

```csharp
public void Stop()
```

#### Remarks

Safe to call from the UI thread itself (e.g. a hotkey handler): in that case the loop is signalled and
unwinds when control returns to it, so we must not <xref href="System.Threading.Thread.Join(System.Int32)" data-throw-if-not-resolved="false"></xref> (a thread cannot join
itself). Only a caller on another thread waits for the UI thread to exit.

### <a id="Jumbee_Console_Dispatcher_VerifyAccess"></a> VerifyAccess\(\)

Throws when the caller is not on the UI thread.

```csharp
public void VerifyAccess()
```

