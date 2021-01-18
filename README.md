# csharp_aop_inject

项目主要演示使用Mono.Cecil工具包如何实现IL中间语言的注入实现。

# 注入工程

该工程是用来IL注入的工程csharp_aop_inject。

核心代码：
```

using System;

namespace csharp_aop_inject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("hello world1");

            Program p = new Program();
            p.add(10, 10);
            p.sub(10, 100);

            Console.WriteLine("hello world2");
        }

        public int add(int v1, int v2)
        {
            int r = 0;
            for (int i = 0; i < 100000; i++) {
                r = v1 + v2 + r;
            }
            return r;
        }

        public int sub(int v1, int v2)
        {
            int r = 0;
            for (int i = 0; i < 1000010; i++) {
                r = v1 - v2 + r;
            }
            return r;
        }
    }
}


```

# 测试工程1

该工程主要目的是给csharp_aop_inject工程中公有实例函数注入性能检测代码，
使用方法为：csharp_aop_inject_test1.exe ./csharp_aop_inject.exe。

核心代码：
```
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace csharp_aop_inject_test
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = args[0];
            if (!File.Exists(filePath)) {
                Console.WriteLine("Param input error.");
                return;
            }
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            if (fileStream != null) {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(fileStream);
                ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;
                Collection<TypeDefinition> typeDefinition = moduleDefinition.Types;
                foreach (TypeDefinition type in typeDefinition) {
                    if (type.IsClass) {
                        foreach (MethodDefinition method in type.Methods) {
                            if (method.IsPublic && !method.IsConstructor) {
                                ILProcessor iLProcessor = method.Body.GetILProcessor();
                                TypeReference stopWatchType = moduleDefinition.ImportReference(typeof(Stopwatch));
                                VariableDefinition variableDefinition = new VariableDefinition(stopWatchType);
                                method.Body.Variables.Add(variableDefinition);
                                Instruction firstInstruction = method.Body.Instructions.First();
                                iLProcessor.InsertBefore(firstInstruction, iLProcessor.Create(OpCodes.Newobj, moduleDefinition.ImportReference(typeof(Stopwatch).GetConstructor(new Type[] { }))));
                                iLProcessor.InsertBefore(firstInstruction, iLProcessor.Create(OpCodes.Stloc_S, variableDefinition));
                                iLProcessor.InsertBefore(firstInstruction, iLProcessor.Create(OpCodes.Ldloc_S, variableDefinition));
                                iLProcessor.InsertBefore(firstInstruction, iLProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(Stopwatch).GetMethod("Start"))));

                                Instruction returnInstruction = method.Body.Instructions.Last();
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Ldloc_S, variableDefinition));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(Stopwatch).GetMethod("Stop"))));

                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Ldstr, $"{method.FullName} run time: "));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Ldloc_S, variableDefinition));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(Stopwatch).GetMethod("get_ElapsedMilliseconds"))));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Box, moduleDefinition.ImportReference(typeof(long))));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Call, moduleDefinition.ImportReference(typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) }))));
                                iLProcessor.InsertBefore(returnInstruction, iLProcessor.Create(OpCodes.Call, moduleDefinition.ImportReference(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));
                            }
                        }
                    }
                }
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                int pointIndex = fileName.LastIndexOf('.');
                string prefixName = fileName.Substring(0, pointIndex);
                string suffixName = fileName.Substring(pointIndex, fileName.Length - pointIndex);
                string newFilePath = Path.Combine(fileInfo.Directory.FullName, prefixName + "_inject" + suffixName);
                assemblyDefinition.Write(newFilePath);
                Console.WriteLine($"IL inject success, output path: {newFilePath}");
                fileStream.Dispose();
            }
        }
    }
}
```

# 测试工程2

该工程主要目的是给csharp_aop_inject工程中Main函数执行前和后分别加入输出，
使用方法为：csharp_aop_inject_test2.exe ./csharp_aop_inject.exe。

核心代码：
```
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace csharp_aop_inject_test2
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = args[0];
            if (!File.Exists(filePath)) {
                Console.WriteLine("Param input error.");
                return;
            }
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            if (fileStream != null) {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(fileStream);
                ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;
                Collection<TypeDefinition> typeDefinition = moduleDefinition.Types;
                foreach (TypeDefinition type in typeDefinition) {
                    foreach (MethodDefinition method in type.Methods) {
                        if (method.Name.Equals("Main")) {
                            Instruction instruction = method.Body.Instructions[0];
                            ILProcessor iLProcessor = method.Body.GetILProcessor();
                            iLProcessor.InsertBefore(instruction, iLProcessor.Create(OpCodes.Ldstr, "Method start..."));
                            iLProcessor.InsertBefore(instruction, iLProcessor.Create(OpCodes.Call, assemblyDefinition.MainModule.ImportReference(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));
                            instruction = method.Body.Instructions[method.Body.Instructions.Count - 1];
                            iLProcessor.InsertBefore(instruction, iLProcessor.Create(OpCodes.Ldstr, "Method finish..."));
                            iLProcessor.InsertBefore(instruction, iLProcessor.Create(OpCodes.Call, assemblyDefinition.MainModule.ImportReference(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));
                        }
                    }
                }
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                int pointIndex = fileName.LastIndexOf('.');
                string prefixName = fileName.Substring(0, pointIndex);
                string suffixName = fileName.Substring(pointIndex, fileName.Length - pointIndex);
                string newFilePath = Path.Combine(fileInfo.Directory.FullName, prefixName + "_inject" + suffixName);
                assemblyDefinition.Write(newFilePath);
                Console.WriteLine($"IL inject success, output path: {newFilePath}");
                fileStream.Dispose();
            }
        }
    }
}
```

# 备注

注入后的输出的文件名称为：csharp_aop_inject_inject.exe
