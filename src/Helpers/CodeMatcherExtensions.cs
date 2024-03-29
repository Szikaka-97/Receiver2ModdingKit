using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Receiver2ModdingKit.Helpers {
    public static class CodeMatcherExtensions {
        public static MethodInfo GetMethod(this System.Type type, string method_signature) {
            // Przepisać żeby działało coś w stylu Wolfire.Receiver2@Receiver2.LinearMover.UpdateDisplay();

            string cut_signature = method_signature;

            BindingFlags flags = BindingFlags.Default;
            Type method_class = null;
            string method_name = "";
            List<Tuple<Type, ParameterModifier>> arguments = new List<Tuple<Type, ParameterModifier>>();


            Match modifier_match = Regex.Match(cut_signature, @"^\s*(public|protected|private)\s*", RegexOptions.Multiline);

            if (modifier_match.Success) {
                flags |= (modifier_match.ToString() == "public") ? BindingFlags.Public : BindingFlags.NonPublic;
            }
            else {
                flags |= BindingFlags.Public | BindingFlags.NonPublic;
            }

            cut_signature = cut_signature.Remove(0, modifier_match.Length);
        

            Match scope_match = Regex.Match(cut_signature, @"^\s*static\s*", RegexOptions.Multiline);

            if (scope_match.Success) {
                flags |= BindingFlags.Static;
            }
            else {
                flags |= BindingFlags.Instance;
            }

            cut_signature = cut_signature.Remove(0, scope_match.Length);
            

            // Get method class name with namespace
            Match class_name_match = Regex.Match(cut_signature, @"(\w|\.)+(?=\.\w+\()", RegexOptions.Multiline);

            if (class_name_match.Success) {
                method_class = Type.GetType(class_name_match.ToString());

                if (method_class == null) {
                    Debug.LogError("Couldn't find a type with name " + class_name_match.ToString() + " Check if you formatted it properly");

                    return null;
                }

                cut_signature = cut_signature.Remove(0, class_name_match.Index + class_name_match.Length + 1);
            }
            else {
                Debug.LogError("Couldn't find class name in method signature " + method_signature + " Check if you formatted it properly");

                return null;
            }
            

            Match method_name_match = Regex.Match(cut_signature, @"\w+(?=\(.+\))");

            if (method_name_match.Success) {
                method_name = method_name_match.ToString();

                cut_signature = cut_signature.Remove(0, class_name_match.Index + class_name_match.Length + 1);
            }
            else {
                Debug.LogError("Couldn't find a method name in method signature " + method_signature + " Check if you formatted it properly");

                return null;
            }

            // Check if the information provided is enough to locate a method
            // If there's only one method with a given name in class, GetMethod() will return it, so we can return it
            // The same goes for when there's no such method, then the function returns null and so we pass it through
            // If there's an ambiguous match, the code doesn't get to return here and continues
            try {
                MethodInfo test_method = method_class.GetMethod(method_name, flags);

                return test_method;
            } catch (AmbiguousMatchException) { } // No luck, let's check the arguments
            
            cut_signature = cut_signature.TrimStart().TrimStart('(').TrimStart();

            if (cut_signature[0] == ')') {
                MethodInfo test_method = method_class.GetMethod(method_name, new Type[] {});

                return test_method;
            }
            else {
                
                Match parameter_modifier_match = Regex.Match(cut_signature, @"(ref|out)\s+(\w+\.\w*)");

                if (parameter_modifier_match.Success) {
                    Type argument_type = Type.GetType(parameter_modifier_match.Groups[1].ToString());

                    if (argument_type == null) {
                        // TODO: Add meaningful error message

                        return null;
                    }

                }
            }

            return null;
        }
    }
}