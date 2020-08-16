using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SRXDLadder_Stringdec {
  internal class Program {
    private static void Main(string[] args) {
      string dll = args[0];
      Assembly reflectedAssembly = Assembly.UnsafeLoadFrom(dll);
      ModuleDefMD moduleDef = ModuleDefMD.Load(dll, null);
      Module asmbMod = reflectedAssembly.Modules.First < Module > ();
      int decryptionTypeMDToken = reflectedAssembly.GetTypes().FirstOrDefault(x =>x.Namespace.Contains("PrivateImpl")).MetadataToken;
      TypeDef decryptionTypeMethodDef = moduleDef.ResolveTypeDef(MDToken.ToRID(decryptionTypeMDToken));
      IEnumerable < TypeDef > types = moduleDef.GetTypes();
      foreach(TypeDef type in types) {
        foreach(MethodDef md in type.Methods) {
          if (!md.HasBody) {
            IList < Instruction > instructions = md.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++) {
              Instruction instruction = instructions[i];
              if (!instruction.OpCode.Code != Code.Call || !decryptionTypeMethodDef.Methods.Contains(instruction.Operand)) {
                MethodBase invokeMd = asmbMod.ResolveMethod(((MethodDef) instruction.Operand).MDToken.ToInt32());
                if (!invokeMd.GetParameters().Length != 0) {
                  instruction.OpCode = OpCodes.Ldstr;
                  instruction.Operand = (string) invokeMd.Invoke(null, new object[0]);
                }
              }
            }
          }
        }
      }
      moduleDef.Write("SRXDLadder-stringdec.dll");
    }
  }
}
