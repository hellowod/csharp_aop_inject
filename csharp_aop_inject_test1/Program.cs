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
