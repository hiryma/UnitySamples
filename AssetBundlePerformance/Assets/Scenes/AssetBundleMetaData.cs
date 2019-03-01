using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// すごいテキトー。ライブラリにする気はまだない。
namespace Kayac
{
	[System.Serializable]
	public class AssetBundleMetaData
	{
		public Hash128 GenerateHash128()
		{
			return (string.IsNullOrEmpty(hash)) ? new Hash128() : Hash128.Parse(hash);
		}
		public string name;
		public string hash;
		public int size;
	}

	[System.Serializable]
	public class AssetBundleMetaDataContainer
	{
		public AssetBundleMetaData[] items;
	}
}
