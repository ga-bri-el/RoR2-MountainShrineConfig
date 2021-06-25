using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MountainShrineConfig
{
    static class Prefabs
    {
		public static List<Type> types;

		public static void LogPrefabs() 
        {
			string outDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Bodys/";
			string text2 = "";
			Log.LogInfo(outDirPath);
			text2 = text2 + outDirPath+ "\n";
			types = new List<Type>();

			var d = ScriptableObject.CreateInstance<RoR2.DirectorCardCategorySelection>();
			foreach (var c in RoR2.ClassicStageInfo.instance.GetComponents<Component>())
            {
				Log.LogInfo($"Stage Component: {c.name}");
            }
			return;
			try
			{
				text2 += "create Directory\n";
				Directory.CreateDirectory(outDirPath);
				text2 += "for each\n";
				string text3;
				foreach (GameObject gameObject in Resources.LoadAll<GameObject>("Prefabs/"))
				{
					Component[] components = gameObject.GetComponents<Component>();
					text3 = "";
					text3 = text3 + gameObject.name + "\n";
					text3 += OutputComponents(components, ">");
					text3 += GetChildren(gameObject.transform, ">");
					File.WriteAllText(outDirPath + gameObject.name + ".txt", text3);
				}
				text2 += "types\n";
				text3 = "";
				text3 += "\n\ntypes\n\n";
				for (int j = 0; j < types.Count; j++)
				{
					MemberInfo[] members = types[j].GetMembers();
					text3 = text3 + types[j].Name + "\n";
					for (int k = 0; k < members.Length; k++)
					{
						bool flag = !(members[k].DeclaringType.Name == "MonoBehaviour") && !(members[k].DeclaringType.Name == "Component") && !(members[k].DeclaringType.Name == "Behaviour") && !(members[k].DeclaringType.Name == "Object");
						if (flag)
						{
							text3 = string.Concat(new string[]
							{
								text3,
								"> ",
								members[k].MemberType.ToString(),
								": ",
								members[k].Name,
								"\n"
							});
						}
					}
				}
				File.WriteAllText(outDirPath + "types.txt", text3);
			}
			catch (Exception ex)
			{
				text2 += ex.ToString();
			}
			File.WriteAllText(outDirPath + "log.txt", text2);
		}

		public static string OutputComponents(Component[] components, string delimi)
		{
			string text = "";
			for (int i = 0; i < components.Length; i++)
			{
				string fullName = components[i].GetType().FullName;
				string text2;
				if (!(fullName == "UnityEngine.Transform"))
				{
					Type type = components[i].GetType();
					text2 = type.FullName + "\n";
					foreach (FieldInfo fieldInfo in type.GetFields())
					{
						text2 = string.Concat(new object[]
						{
							text2,
							delimi,
							"v ",
							fieldInfo.Name,
							" = ",
							fieldInfo.GetValue(components[i]),
							"\n"
						});
					}
				}
				else
				{
					Transform transform = (Transform)components[i];
					text2 = string.Concat(new string[]
					{
						"transform = p:",
						transform.localPosition.ToString(),
						" r:",
						transform.eulerAngles.ToString(),
						" s:",
						transform.localScale.ToString(),
						"\n"
					});
				}
				text = string.Concat(new string[]
				{
					text,
					"\n",
					delimi,
					" ",
					text2
				});
				bool flag = types.Contains(components[i].GetType());
				if (flag)
				{
					types.Add(components[i].GetType());
				}
			}
			return text;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000024D4 File Offset: 0x000006D4
		public static string GetChildren(Transform transform, string delimi)
		{
			string text = "";
			for (int i = 0; i < transform.childCount; i++)
			{
				GameObject gameObject = transform.GetChild(i).gameObject;
				text = string.Concat(new string[]
				{
					text,
					delimi,
					"c ",
					gameObject.name,
					"\n"
				});
				Component[] components = gameObject.GetComponents<Component>();
				text += OutputComponents(components, delimi + ">");
				text += GetChildren(transform.GetChild(i), delimi + ">");
			}
			return text;
		}
	}
}
