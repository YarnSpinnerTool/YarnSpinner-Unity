tasks = ["System.Threading.Tasks.Task", "Cysharp.Threading.Tasks.UniTask", "UnityEngine.Awaitable", "YarnTask"]
types = ["int","float","double","sbyte","byte","short","ushort","uint","long","ulong","decimal","string","bool"]

for type in types:
    print(f"{type} st => new FunctionDispatchSucceededSynchronously(st),")

for task in tasks:
    for type in types:
        print(f"{task}<{type}> at => new FunctionDispatchSucceededAsynchronously<{type}>(at),")