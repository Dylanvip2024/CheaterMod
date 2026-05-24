using FullBrightMod.Core;
using UnityEngine;
using System.IO;
using System.Linq;

namespace FullBrightMod.Modules
{
    public static class BoomboxStyles
    {
        private static GUIStyle _customButtonStyle;
        private static GUIStyle _geekLabelStyle;

        private static Texture2D MakeColorTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static GUIStyle GeekLabelStyle
        {
            get
            {
                if (_geekLabelStyle == null)
                {
                    _geekLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 10,
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _geekLabelStyle;
            }
        }

        public static GUIStyle CustomButtonStyle
        {
            get
            {
                if (_customButtonStyle == null)
                {
                    _customButtonStyle = new GUIStyle(GUI.skin.button);
                    _customButtonStyle.normal.background = MakeColorTexture(2, 2, new Color(0.25f, 0.25f, 0.25f, 0.9f));
                    _customButtonStyle.hover.background = MakeColorTexture(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f));
                    _customButtonStyle.active.background = MakeColorTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
                    _customButtonStyle.focused.background = _customButtonStyle.normal.background;
                    _customButtonStyle.onNormal.background = _customButtonStyle.normal.background;
                    _customButtonStyle.fontSize = 12;
                    _customButtonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                    _customButtonStyle.hover.textColor = Color.white;
                    _customButtonStyle.active.textColor = new Color(0.5f, 0.5f, 0.5f);
                    _customButtonStyle.border = new RectOffset(0, 0, 0, 0);
                    _customButtonStyle.padding = new RectOffset(0, 0, 0, 0);
                    _customButtonStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _customButtonStyle;
            }
        }
    }

    public class HumanBoombox : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_human_boombox") ?? "人形音响";
        public override ModuleCategory Category => ModuleCategory.Misc;

        private string _musicFolderPath;

        public override void OnEnable() 
        {
            Settings.IsBoomboxEnabled = true;
            RefreshMusicList();
        }
        
        public override void OnDisable() => Settings.IsBoomboxEnabled = false;

        public HumanBoombox()
        {
            _musicFolderPath = Path.Combine(Application.dataPath, "../music");
            if (!Directory.Exists(_musicFolderPath))
            {
                Directory.CreateDirectory(_musicFolderPath);
            }
            RefreshMusicList();
        }

        private void RefreshMusicList()
        {
            if (Directory.Exists(_musicFolderPath))
            {
                string[] files = Directory.GetFiles(_musicFolderPath, "*.*")
                    .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav") || s.EndsWith(".ogg")).ToArray();
                
                Settings.AvailableMusicTracks.Clear();
                foreach (string file in files)
                {
                    Settings.AvailableMusicTracks.Add(Path.GetFileName(file));
                }
            }
            if (Settings.CurrentTrackIndex >= Settings.AvailableMusicTracks.Count)
            {
                Settings.CurrentTrackIndex = 0;
            }
        }

        public override float GetSettingsHeight() => 30f; // 缩减高度，只需一行

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            GUIStyle labelStyle = BoomboxStyles.GeekLabelStyle;
            GUIStyle buttonStyle = BoomboxStyles.CustomButtonStyle;

            // 只保留歌曲切换器
            Rect trackLabelRect = new Rect(x + 8, y, 70, 22);
            GUI.Label(trackLabelRect, $"  {Utils.I18n.Get("set_boombox_track") ?? "当前曲目"}", labelStyle);

            if (Settings.AvailableMusicTracks.Count > 0)
            {
                if (GUI.Button(new Rect(x + 78, y + 2, 22, 18), "‹", buttonStyle))
                {
                    Settings.CurrentTrackIndex--;
                    if (Settings.CurrentTrackIndex < 0) Settings.CurrentTrackIndex = Settings.AvailableMusicTracks.Count - 1;
                }

                string trackName = Settings.AvailableMusicTracks[Settings.CurrentTrackIndex];
                if (trackName.Length > 20) trackName = trackName.Substring(0, 17) + "...";
                
                GUIStyle centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                GUI.Label(new Rect(x + 100, y, width - 130, 22), trackName, centerStyle);

                if (GUI.Button(new Rect(x + width - 28, y + 2, 22, 18), "›", buttonStyle))
                {
                    Settings.CurrentTrackIndex++;
                    if (Settings.CurrentTrackIndex >= Settings.AvailableMusicTracks.Count) Settings.CurrentTrackIndex = 0;
                }
            }
            else
            {
                GUIStyle warningStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.8f, 0.4f, 0.4f) } };
                GUI.Label(new Rect(x + 78, y, width - 85, 22), "(music文件夹为空)", warningStyle);
            }
            
            y += 35f;
        }
    }
}