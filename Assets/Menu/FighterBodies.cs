using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace BattleTv.Menu
{
    /// <summary>
    /// One fighter is a folder under StreamingAssets/fighters/&lt;id&gt;.
    /// A match loads only the folders on the card. Fight numbers stay in LaneRoster.
    /// </summary>
    public static class FighterBodies
    {
        public sealed class Clip
        {
            public string Name;
            public float Seconds;
            public float Pivot;
            public float Pace;
            public Sprite[] Sprites;
        }

        public sealed class Body
        {
            public string Id;
            public bool FlipWithFoe;
            public bool ClipPivot;
            public Sprite Idle;
            public Clip Walk;
            public float WalkSeconds;
            public Clip Find(string callout)
            {
                if (string.IsNullOrEmpty(callout))
                    return null;
                for (var i = 0; i < _clips.Count; i++)
                {
                    if (_clips[i].Name == callout)
                        return _clips[i];
                }
                return null;
            }

            public Clip End(bool win)
            {
                return Find(win ? "win" : "death");
            }

            internal readonly List<Clip> _clips = new List<Clip>(8);
            internal bool Full;
        }

        [Serializable]
        class FileDto
        {
            public string id;
            public string flip;
            public bool clipPivot;
            public float walk;
            public string idle;
            public float idlePivot;
            public ClipDto[] clips;
        }

        [Serializable]
        class ClipDto
        {
            public string name;
            public float seconds;
            public float pivot;
            public float pace;
            public string[] frames;
            public float[] pivots;
        }

        static readonly Dictionary<string, Body> Have = new Dictionary<string, Body>(16);
        static readonly HashSet<string> Missing = new HashSet<string>();

        public static Body Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            Have.TryGetValue(id, out var body);
            return body;
        }

        public static bool Settled(string id)
        {
            // An unpainted book kit is a deliberate mark. It has no folder to
            // request, so it is settled without a network round-trip.
            return string.IsNullOrEmpty(id) || !LaneRoster.Painted(id) ||
                   Have.ContainsKey(id) || Missing.Contains(id);
        }

        public static IEnumerator LoadIdle(string id)
        {
            if (string.IsNullOrEmpty(id) || !LaneRoster.Painted(id) ||
                Have.ContainsKey(id) || Missing.Contains(id))
                yield break;
            yield return LoadOne(id, false);
        }

        public static IEnumerator Load(IList<string> ids)
        {
            if (ids == null)
                yield break;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id) || !LaneRoster.Painted(id))
                    continue;
                if (Have.TryGetValue(id, out var body) && body.Full)
                    continue;
                Missing.Remove(id);
                yield return LoadOne(id, true);
            }
        }

        static IEnumerator LoadOne(string id, bool full)
        {
            var jsonReq = UnityWebRequest.Get(Url(id, "entity.json"));
            yield return jsonReq.SendWebRequest();
            if (jsonReq.result != UnityWebRequest.Result.Success)
            {
                Missing.Add(id);
                jsonReq.Dispose();
                yield break;
            }
            var dto = JsonUtility.FromJson<FileDto>(jsonReq.downloadHandler.text);
            jsonReq.Dispose();
            if (dto == null || string.IsNullOrEmpty(dto.idle))
            {
                Missing.Add(id);
                yield break;
            }
            var paths = new List<string> { dto.idle };
            if (full && dto.clips != null)
            {
                for (var c = 0; c < dto.clips.Length; c++)
                {
                    var frames = dto.clips[c].frames;
                    if (frames == null)
                        continue;
                    for (var f = 0; f < frames.Length; f++)
                    {
                        if (!string.IsNullOrEmpty(frames[f]) && !paths.Contains(frames[f]))
                            paths.Add(frames[f]);
                    }
                }
            }
            var textures = new Dictionary<string, Texture2D>(paths.Count);
            var requests = new UnityWebRequest[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                requests[i] = UnityWebRequest.Get(Url(id, paths[i]));
                requests[i].SendWebRequest();
            }
            var waiting = true;
            while (waiting)
            {
                waiting = false;
                for (var i = 0; i < requests.Length; i++)
                {
                    if (requests[i] != null && !requests[i].isDone)
                        waiting = true;
                }
                if (waiting)
                    yield return null;
            }
            for (var i = 0; i < requests.Length; i++)
            {
                var req = requests[i];
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    if (ImageConversion.LoadImage(tex, req.downloadHandler.data))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        tex.Apply(false, true);
                        textures[paths[i]] = tex;
                    }
                    else
                        UnityEngine.Object.Destroy(tex);
                }
                req.Dispose();
            }
            if (!textures.ContainsKey(dto.idle))
            {
                Missing.Add(id);
                yield break;
            }
            var body = new Body
            {
                Id = string.IsNullOrEmpty(dto.id) ? id : dto.id,
                FlipWithFoe = dto.flip == "with",
                ClipPivot = dto.clipPivot,
                Idle = SpriteOf(textures[dto.idle], dto.idlePivot > 0.001f ? dto.idlePivot : 0.5f),
                WalkSeconds = dto.walk > 0.01f ? dto.walk : 0.62f,
                Full = full
            };
            if (full && dto.clips != null)
            {
                for (var c = 0; c < dto.clips.Length; c++)
                {
                    var spec = dto.clips[c];
                    if (spec == null || spec.frames == null || spec.frames.Length == 0)
                        continue;
                    var sprites = new Sprite[spec.frames.Length];
                    var ready = true;
                    for (var f = 0; f < spec.frames.Length; f++)
                    {
                        if (!textures.TryGetValue(spec.frames[f], out var tex))
                        {
                            ready = false;
                            break;
                        }
                        var px = spec.pivots != null && f < spec.pivots.Length ? spec.pivots[f] : 0.5f;
                        sprites[f] = SpriteOf(tex, px);
                    }
                    if (!ready)
                        continue;
                    var clip = new Clip
                    {
                        Name = spec.name,
                        Seconds = spec.seconds,
                        Pivot = spec.pivot,
                        Pace = spec.pace,
                        Sprites = sprites
                    };
                    body._clips.Add(clip);
                    if (spec.name == "walk")
                        body.Walk = clip;
                }
            }
            Have[id] = body;
            Missing.Remove(id);
        }

        static Sprite SpriteOf(Texture2D tex, float pivotX)
        {
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(Mathf.Clamp01(pivotX), 0f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        static string Url(string id, string file)
        {
            var path = Application.streamingAssetsPath + "/fighters/" + id + "/" + file;
            if (path.Contains("://"))
                return path;
            return "file:///" + path.Replace('\\', '/');
        }
    }
}
