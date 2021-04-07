using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SRXDLadder_Stringdec
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string file = args[0];

            Module reflectedModule = Assembly.LoadFrom(file).Modules.First();
            ModuleDefinition moduleDefinition = ModuleDefinition.FromFile(file);

            IEnumerable<TypeDefinition> types = moduleDefinition.GetAllTypes();

            int decryptorTypeToken = types.First(type => type.FullName.Contains("PrivateImplementation")).MetadataToken.ToInt32();

            foreach (TypeDefinition type in types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.CilMethodBody == null)
                    {
                        continue;
                    }

                    foreach (CilInstruction instruction in method.CilMethodBody.Instructions)
                    {
                        if (instruction.OpCode.Code != CilCode.Call || instruction.Operand is not MethodDefinition methodDefinition || methodDefinition.Parameters.Count != 0)
                        {
                            continue;
                        }

                        int methodToken = methodDefinition.DeclaringType.MetadataToken.ToInt32();
                        
                        if (methodToken != decryptorTypeToken)
                        {
                            continue;
                        }

                        MethodBase reflectedMethod = reflectedModule.ResolveMethod(methodDefinition.MetadataToken.ToInt32());
                        instruction.OpCode = CilOpCodes.Ldstr;
                        Console.WriteLine(instruction.Operand = reflectedMethod.Invoke(null, Array.Empty<object>()));
                    }
                }
            }
            moduleDefinition.Write("SRXDLadder-decrypted.dll");
        }
    }
}
