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
