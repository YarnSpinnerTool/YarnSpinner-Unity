from num2words import num2words

# at least initially I want these like this because until I also add them into the cs files automatically I want to be able to copy-paste them one at a time
# or at least have the option to do so
# this does mean there is gonna be some duplicates
# wow this code is terrible though
def gyb(count, template):
    for i in range(count):
        r = ["T" + str(x) for x in range(1, i + 2)]
        s = ", ".join(r)
        print(template.format(s))

# the docstring for this one is a butt, hence it's own function
def gyb2(count, body, header0, header1):
    
    for i in range(count):
        template = header0
        if (i > 0):
            template = header1
        template += body

        s = ", ".join(["T" + str(x) for x in range(1, i + 2)])
        t = ",".join(["T" + str(x) for x in range(1, i + 1)])
        print(template.format(s, i + 1, num2words(i + 1, to = 'ordinal'), t))

# gybs for Actions.cs
actionscsGybs = {
    "GYB1": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);",
    "GYB2": "public void AddCommandHandler<{0}>(string commandName, Action<{0}> handler) => AddCommandHandler(commandName, (Delegate)handler);",
    "GYB3": "public void AddFunction<{0}, TResult>(string name, Func<{0}, TResult> implementation) => AddFunction(name, (Delegate)implementation);",
    "GYB4": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, IEnumerator> handler)\n{{\n\tthis.AddCommandHandler(commandName, (Delegate)handler);\n}}",
    "GYB5": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);",
    "GYB6": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);",
    "GYB7": "public void AddCommandHandler<{0}>(string commandName, Action<{0}> handler) => AddCommandHandler(commandName, (Delegate)handler);",
    "GYB8": "public void AddFunction<{0}, TResult>(string name, Func<{0}, TResult> implementation) => AddFunction(name, (Delegate)implementation);",
}

# gybs for IActionRegistration.cs
iactionregistrationcsGybs = {
    "GYB9": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\nvoid AddCommandHandler<{0}>(string commandName, System.Func<{0}, Coroutine> handler);",
    "GYB10": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\nvoid AddCommandHandler<{0}>(string commandName, System.Func<{0}, IEnumerator> handler);",
    "GYB11": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\nvoid AddCommandHandler<{0}>(string commandName, System.Action<{0}> handler);",
}

# gybs for dialoguerunner.cs
dialoguerunnerGybs = {
    "GYB13": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\npublic void AddCommandHandler<{0}>(string commandName, System.Func<{0}, Coroutine> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);",
    "GYB14": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\npublic void AddCommandHandler<{0}>(string commandName, System.Func<{0}, IEnumerator> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);",
    "GYB15": "/// <inheritdoc cref=\"AddCommandHandler(string, Delegate)\"/>\npublic void AddCommandHandler<{0}>(string commandName, System.Action<{0}> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);",
}

# gybs for commandswindow.cs
commandswindowGybs = {
    "GYB17": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, Coroutine> handler)\n{{\n\tAddCommandHandler(commandName, (Delegate)handler);\n}}",
    "GYB18": "public void AddCommandHandler<{0}>(string commandName, Func<{0}, IEnumerator> handler)\n{{\n\tAddCommandHandler(commandName, (Delegate)handler);\n}}",
    "GYB19": "public void AddCommandHandler<{0}>(string commandName, Action<{0}> handler)\n{{\n\tAddCommandHandler(commandName, (Delegate)handler);\n}}",
    "GYB20": "public void AddFunction<{0}, TResult>(string name, Func<{0}, TResult> implementation)\n{{\n\tAddFunction(name, (Delegate)implementation);\n}}",
}

for key in sorted(actionscsGybs):
    print(key)
    gyb(10, actionscsGybs[key])
    print("---")

for key in sorted(iactionregistrationcsGybs):
    print(key)
    gyb(10, iactionregistrationcsGybs[key])
    print("---")
# this one is also iactionregistraction but it's weird
print("GYB12")
gyb2(10, "/// <typeparam name=\"T{1}\">The type of the {2} parameter to the function.</typeparam>\nvoid AddFunction<{0}, TResult>(string name, System.Func<{0}, TResult> implementation);", "/// <inheritdoc cref=\"AddFunction{{TResult}}(string, Func{{TResult}})\" />\n", "/// <inheritdoc cref=\"AddFunction{{{3},TResult}}(string, Func{{{3},TResult}})\" />\n")
print("---")

for key in sorted(dialoguerunnerGybs):
    print(key)
    gyb(10, dialoguerunnerGybs[key])
    print("---")
# this one is also dialogue runner but is weird
print("GYB16")
gyb2(10, "/// <typeparam name=\"T{1}\">The type of the {2} parameter to the function.</typeparam>\npublic void AddFunction<{0}, TResult>(string name, System.Func<{0}, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);", "/// <inheritdoc cref=\"AddFunction{{TResult}}(string, Func{{TResult}})\" />\n", "/// <inheritdoc cref=\"AddFunction{{{3},TResult}}(string, Func{{{3},TResult}})\" />\n")
print("---")

for key in sorted(commandswindowGybs):
    print(key)
    a = commandswindowGybs[key]
    gyb(10, a)
    print("---")
