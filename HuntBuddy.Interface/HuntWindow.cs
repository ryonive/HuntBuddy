using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using FlexConfig;

namespace HuntBuddy.Interface
{
	public class HuntWindow
	{
		public Func<string>? GetPluginName { get; set; }
		public Func<uint, uint>? GetCurrentKills { get; set; }
		public Func<bool>? IsMobHuntEntriesReady { get; set; }
		public Func<bool>? IsTeleporterConsumerSubscribed { get; set; }
		public Func<uint, bool>? LocationContainsKey { get; set; }

		public Func<Dictionary<string, Dictionary<KeyValuePair<uint, string>, List<MobHuntEntry>>>>? GetMobHuntEntries =
			null!;

		public Func<List<MobHuntEntry>>? GetCurrentAreaMobHuntEntries { get; set; }

		public event Action? Reload;

		public event Action<MobHuntEntry>? PlaceMapMarker;

		public event Action<MobHuntEntry>? PlaceMapMarkerAndShowMap;

		public event Action<MobHuntEntry>? PlaceMapMarkerAndShowSpecialMap;

		public event Action<MobHuntEntry>? TeleportToNearestAetheryte;

		#region Configuration

		private readonly Configuration configuration;

		private uint iconBackgroundColourU32;

		#endregion

		public bool DrawInterface;
		private bool drawConfigurationInterface;

		public HuntWindow(Configuration configuration)
		{
			this.configuration = configuration;

			if (!this.configuration.ContainsKey("IconScale"))
			{
				this.configuration.Set("IconScale", 1.0f);
			}

			if (!this.configuration.ContainsKey("IconBackgroundColour"))
			{
				this.configuration.Set("IconBackgroundColour", new Vector4(0.76f, 0.75f, 0.76f, 0.8f));
			}

			this.iconBackgroundColourU32 =
				ImGui.ColorConvertFloat4ToU32(this.configuration.Get("IconBackgroundColour"));
		}

		public bool Draw()
		{
			var draw = true;

			ImGui.SetNextWindowSize(new Vector2(400 * ImGui.GetIO().FontGlobalScale, 500), ImGuiCond.Once);

			if (!ImGui.Begin($"{this.GetPluginName?.Invoke() ?? string.Empty}", ref draw, ImGuiWindowFlags.NoDocking))
			{
				return draw;
			}

			if (this.IsMobHuntEntriesReady == null || !this.IsMobHuntEntriesReady.Invoke())
			{
				ImGui.Text("Reloading data ...");
				ImGui.End();
				return draw;
			}

			if (IconButton(FontAwesomeIcon.Redo, "Reload"))
			{
				ImGui.End();
				this.Reload?.Invoke();
				return draw;
			}

			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.Text("Click this button to reload daily hunt data");
				ImGui.EndTooltip();
			}

			ImGui.SameLine();

			if (IconButton(FontAwesomeIcon.Cog, "Config"))
			{
				this.drawConfigurationInterface = !this.drawConfigurationInterface;
			}

			if (this.GetMobHuntEntries != null)
			{
				foreach (var expansionEntry in this.GetMobHuntEntries.Invoke()
					         .Where(expansionEntry => ImGui.TreeNode(expansionEntry.Key)))
				{
					foreach (var entry in expansionEntry.Value.Where(
						         entry =>
						         {
							         var treeOpen = ImGui.TreeNodeEx(
								         entry.Key.Value,
								         ImGuiTreeNodeFlags.AllowItemOverlap);
							         ImGui.SameLine();
							         var killedCount = entry.Value.Count(
								         x => this.GetCurrentKills?.Invoke(x.CurrentKillsOffset) == x.NeededKills);

							         if (killedCount != entry.Value.Count)
							         {
								         ImGui.Text($"({killedCount}/{entry.Value.Count})");
							         }
							         else
							         {
								         ImGui.TextColored(
									         new Vector4(0f, 1f, 0f, 1f),
									         $"({killedCount}/{entry.Value.Count})");
							         }

							         return treeOpen;
						         }))
					{
						ImGui.Indent();
						foreach (var mobHuntEntry in entry.Value)
						{
							if (this.LocationContainsKey?.Invoke(mobHuntEntry.MobHuntId) ?? false)
							{
								if (IconButton(FontAwesomeIcon.MapMarkerAlt, $"pin##{mobHuntEntry.MobHuntId}"))
								{
									this.PlaceMapMarker?.Invoke(mobHuntEntry);
								}

								if (ImGui.IsItemHovered())
								{
									ImGui.BeginTooltip();
									ImGui.Text("Place marker on the map");
									ImGui.EndTooltip();
								}

								ImGui.SameLine();

								if (IconButton(FontAwesomeIcon.Compass, $"openRadius##{mobHuntEntry.MobHuntId}"))
								{
									this.PlaceMapMarkerAndShowSpecialMap?.Invoke(mobHuntEntry);
								}

								if (ImGui.IsItemHovered())
								{
									ImGui.BeginTooltip();
									ImGui.Text("Show hunt location on the map");
									ImGui.EndTooltip();
								}

								ImGui.SameLine();

								if (IconButton(FontAwesomeIcon.MapMarkedAlt, $"open##{mobHuntEntry.MobHuntId}"))
								{
									this.PlaceMapMarkerAndShowMap?.Invoke(mobHuntEntry);
								}

								if (ImGui.IsItemHovered())
								{
									ImGui.BeginTooltip();
									ImGui.Text("Show hunt location on the map");
									ImGui.EndTooltip();
								}

								ImGui.SameLine();

								if (this.IsTeleporterConsumerSubscribed != null &&
								    this.IsTeleporterConsumerSubscribed.Invoke())
								{
									if (IconButton(FontAwesomeIcon.StreetView, $"t##{mobHuntEntry.MobHuntId}"))
									{
										this.TeleportToNearestAetheryte?.Invoke(mobHuntEntry);
									}

									if (ImGui.IsItemHovered())
									{
										ImGui.BeginTooltip();
										ImGui.Text("Teleport to nearest aetheryte");
										ImGui.EndTooltip();
									}

									ImGui.SameLine();
								}
							}

							var currentKills = this.GetCurrentKills?.Invoke(mobHuntEntry.CurrentKillsOffset);
							ImGui.Text(mobHuntEntry.Name);
							if (ImGui.IsItemHovered())
							{
								ImGui.PushStyleColor(ImGuiCol.PopupBg, Vector4.Zero);
								ImGui.BeginTooltip();
								this.DrawHuntIcon(mobHuntEntry);
								ImGui.PopStyleColor();
								ImGui.EndTooltip();
							}

							ImGui.SameLine();
							if (currentKills != mobHuntEntry.NeededKills)
							{
								ImGui.Text($"({currentKills}/{mobHuntEntry.NeededKills})");
							}
							else
							{
								ImGui.TextColored(
									new Vector4(0f, 1f, 0f, 1f),
									$"({currentKills}/{mobHuntEntry.NeededKills})");
							}
						}

						ImGui.Unindent();
						ImGui.TreePop();
					}

					ImGui.TreePop();
				}
			}

			ImGui.End();

			if (this.drawConfigurationInterface)
			{
				this.DrawConfiguration();
			}

			return draw;
		}

		public void DrawLocalHunts()
		{
			if (this.GetCurrentAreaMobHuntEntries == null)
			{
				return;
			}

			var currentAreaMobHuntEntries = this.GetCurrentAreaMobHuntEntries.Invoke();
			if (!this.configuration.Get("ShowLocalHunts", false) ||
			    currentAreaMobHuntEntries.Count(
				    x =>
					    this.GetCurrentKills?.Invoke(x.CurrentKillsOffset) == x.NeededKills) ==
			    currentAreaMobHuntEntries.Count)
			{
				return;
			}

			ImGui.SetNextWindowSize(Vector2.Zero, ImGuiCond.Always);

			var windowFlags = ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoDocking;

			if (this.configuration.Get("HideLocalHuntBackground", false))
			{
				windowFlags |= ImGuiWindowFlags.NoBackground;
			}

			if (!ImGui.Begin("Hunts in current area", windowFlags))
			{
				return;
			}

			foreach (var mobHuntEntry in currentAreaMobHuntEntries)
			{
				var currentKills = this.GetCurrentKills?.Invoke(mobHuntEntry.CurrentKillsOffset);

				if (this.configuration!.Get("HideCompletedHunts", false) && currentKills == mobHuntEntry.NeededKills)
				{
					continue;
				}

				if (this.LocationContainsKey != null && this.LocationContainsKey.Invoke(mobHuntEntry.MobHuntId))
				{
					if (IconButton(FontAwesomeIcon.MapMarkerAlt, $"pin##{mobHuntEntry.MobHuntId}"))
					{
						this.PlaceMapMarker?.Invoke(mobHuntEntry);
					}

					if (ImGui.IsItemHovered())
					{
						ImGui.BeginTooltip();
						ImGui.Text("Place marker on the map");
						ImGui.EndTooltip();
					}

					ImGui.SameLine();

					if (IconButton(FontAwesomeIcon.Compass, $"openRadius##{mobHuntEntry.MobHuntId}"))
					{
						this.PlaceMapMarkerAndShowSpecialMap?.Invoke(mobHuntEntry);
					}

					if (ImGui.IsItemHovered())
					{
						ImGui.BeginTooltip();
						ImGui.Text("Show hunt location on the map");
						ImGui.EndTooltip();
					}

					ImGui.SameLine();

					if (IconButton(FontAwesomeIcon.MapMarkedAlt, $"open##{mobHuntEntry.MobHuntId}"))
					{
						this.PlaceMapMarkerAndShowMap?.Invoke(mobHuntEntry);
					}

					if (ImGui.IsItemHovered())
					{
						ImGui.BeginTooltip();
						ImGui.Text("Show hunt location on the map");
						ImGui.EndTooltip();
					}

					ImGui.SameLine();
				}

				ImGui.Text($"{mobHuntEntry.Name} ({currentKills}/{mobHuntEntry.NeededKills})");

				if (this.configuration.Get("ShowLocalHuntIcons", false))
				{
					this.DrawHuntIcon(mobHuntEntry);
				}
			}

			ImGui.End();
		}

		private void Checkbox(string label, string key)
		{
			var value = this.configuration.Get(key, default(bool));
			
			if (!ImGui.Checkbox(label, ref value.Reference))
			{
				return;
			}

			this.configuration.Set(key, value);
		}

		private void SliderFloat(
			string label,
			string key,
			float min,
			float max,
			string? format)
		{
			var value = this.configuration.Get(key, 0.0f);
			
			if (!ImGui.SliderFloat(label, ref value.Reference, min, max, format ?? "%.3f"))
			{
				return;
			}

			this.configuration.Set(key, value);
		}

		private void ColorEdit4(string label, string key)
		{
			var value = this.configuration.Get(key, default(Vector4));
			
			if (!ImGui.ColorEdit4(label, ref value.Reference))
			{
				return;
			}

			this.iconBackgroundColourU32 = ImGui.ColorConvertFloat4ToU32(value);
			this.configuration.Set(key, value);
		}

		private void DrawConfiguration()
		{
			ImGui.SetNextWindowSize(Vector2.Zero, ImGuiCond.Always);

			if (!ImGui.Begin($"{this.GetPluginName?.Invoke() ?? string.Empty} settings", ImGuiWindowFlags.NoDocking))
			{
				return;
			}

			this.Checkbox("Show hunts in local area", "ShowLocalHunts");
			this.Checkbox("Show icons of hunts in local area", "ShowLocalHuntIcons");
			this.Checkbox("Hide background of local hunts window", "HideLocalHuntBackground");
			this.Checkbox("Hide completed targets in local hunts window", "HideCompletedHunts");
			this.SliderFloat(
				"Hunt icon scale",
				"IconScale",
				0.2f,
				2f,
				"%.2f");
			this.ColorEdit4("Hunt icon background colour", "IconBackgroundColour");

			ImGui.End();
		}

		private static bool IconButton(FontAwesomeIcon icon, string? id = null)
		{
			ImGui.PushFont(UiBuilder.IconFont);

			var text = icon.ToIconString();
			if (id != null)
			{
				text += $"##{id}";
			}

			var result = ImGui.Button(text);

			ImGui.PopFont();

			return result;
		}

		private void DrawHuntIcon(MobHuntEntry mobHuntEntry)
		{
			var cursorPos = ImGui.GetCursorScreenPos();
			var imageSize = mobHuntEntry.ExpansionId < 3 ? new Vector2(192f, 128f) : new Vector2(210f);
			imageSize *= ImGui.GetIO().FontGlobalScale * this.configuration.Get("IconScale", 1.0f);

			ImGui.InvisibleButton("canvas", imageSize);

			var drawList = ImGui.GetWindowDrawList();
			if (mobHuntEntry.ExpansionId == 4 && !mobHuntEntry.IsEliteMark) // Endwalker uses circle for non elite mobs
			{
				drawList.AddCircleFilled(
					cursorPos + (imageSize / 2f),
					imageSize.X / 2f,
					this.iconBackgroundColourU32);
			}
			else
			{
				drawList.AddRectFilled(
					cursorPos,
					cursorPos + imageSize,
					this.iconBackgroundColourU32);
			}

			if (mobHuntEntry.Icon != null)
			{
				drawList.AddImage(mobHuntEntry.Icon.ImGuiHandle, cursorPos, cursorPos + imageSize);
			}
		}
	}
}