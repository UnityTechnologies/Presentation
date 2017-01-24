using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

namespace Unity.Presentation 
{

	/// <summary>
	/// Global properties for all presentations.
	/// </summary>
	public class Properties : ScriptableObject 
	{

		private const string ASSET_NAME = "Properties.asset";
		private const string ASSET_FOLDER = "Assets/Presentation/Editor";

		static Properties instance;

		public static Properties Instance
		{
			get 
			{
				if (instance == null)
				{
					var path = Path.Combine(Utils.PackageRoot, ASSET_NAME);
					instance = AssetDatabase.LoadAssetAtPath<Properties>(path);

					if (instance == null)
					{
						instance = CreateInstance<Properties>();
						AssetDatabase.CreateAsset(instance, path);
					}
				}

				return instance;
			}
		}

		public KeyCode NextSlide = KeyCode.RightArrow;
		public KeyCode PreviousSlide = KeyCode.LeftArrow;

	}
}