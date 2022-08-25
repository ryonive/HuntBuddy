﻿using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Configuration;

namespace HuntBuddy
{
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; }

		public bool ShowLocalHunts;
		public bool ShowLocalHuntIcons;
		public bool HideLocalHuntBackground;
		public bool HideCompletedHunts;
		public float IconScale = 1f;
		public Vector4 IconBackgroundColour = new(0.76f, 0.75f, 0.76f, 0.8f);

		public void Save()
		{
			Plugin.PluginInterface.SavePluginConfig(this);
		}
	}
}