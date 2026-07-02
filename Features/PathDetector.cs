using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class PathDetector
    {
        // Menyimpan nama game dan developer (Sistem bawaan Fase 1)
        public static string GameName { get; private set; }
        public static string DeveloperName { get; private set; }

        // Variabel kontrol dan penampung data scanner
        public static bool IsEnabled = true;
        public static bool IsAdvanced = false;

        public static string LastScannedSpriteName = "";

        // Menambahkan variabel penampung untuk rangkuman hasil akhir
        public static string PickedSpriteName = "";
        public static string PickedPath = "";
        public static string PickedObjectName = "";

        // Method untuk inisialisasi awal
        public static void Initialize()
        {
            // Mengambil info dari engine Unity
            GameName = Application.productName;
            DeveloperName = Application.companyName;

            // Log informasi dalam bahasa inggris
            Main.Logger.LogInfo($"Game Detected: {GameName}");
            Main.Logger.LogInfo($"Developer Detected: {DeveloperName}");
            Main.Logger.LogInfo($"Game Path: {Application.dataPath}");

            // Membuat GameObject tersembunyi agar fungsi HandleInput dapat berjalan di Update loop
            GameObject handlerObj = new GameObject("Alaska_PathDetectorHandler");
            Object.DontDestroyOnLoad(handlerObj);
            handlerObj.AddComponent<PathDetectorHandler>();

            Main.Logger.LogInfo("Path Detector with Advanced Scanner and Cursor Unlocker initialized.");
        }

        // Komponen MonoBehaviour internal untuk mendeteksi input setiap frame
        private class PathDetectorHandler : MonoBehaviour
        {
            void Update()
            {
                PathDetector.HandleInput();
            }
        }

        // Fungsi utama untuk mendeteksi input dari pengguna
        public static void HandleInput()
        {
            if (!IsEnabled) return;

            // [FITUR BARU] Toggle Kursor Bebas dengan tombol F8
            if (Input.GetKeyDown(KeyCode.F8))
            {
                // Jika kursor sedang dikunci/disembunyikan oleh game, kita paksa muncul!
                if (Cursor.lockState == CursorLockMode.Locked || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Main.Logger.LogInfo("🔓 [Dev Mode] Cursor UNLOCKED and VISIBLE for scanning!");
                }
                // Jika ditekan lagi, kembalikan ke game
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    Main.Logger.LogInfo("🔒 [Dev Mode] Cursor LOCKED back to gameplay.");
                }
            }

            // [PERBAIKAN] Tekan Ctrl Kanan + Klik Kanan untuk Scan Teks (Aman dari jongkok & lari!)
            if (Input.GetKey(KeyCode.RightControl) && Input.GetMouseButtonDown(1))
            {
                ScanObjectUnderMouse();
            }

            // Tekan Alt Kiri/Kanan + Klik Kanan untuk Scan Tekstur
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButtonDown(1))
            {
                PickTextureUnderMouse();
            }
        }

        // Scanner Tekstur/Gambar (Filter Area & Daftar Hitam)
        private static void PickTextureUnderMouse()
        {
            PickedSpriteName = "";
            PickedPath = "";
            PickedObjectName = "";
            int foundCount = 0;
            Vector2 mousePos = Input.mousePosition;

            Main.Logger.LogInfo("---------------------------------------------");
            Main.Logger.LogInfo("Texture Picker - Scanning for Sprites/Images");
            Main.Logger.LogInfo("---------------------------------------------");

            float smallestArea = float.MaxValue;

            // 1. ABSOLUTE UI SCANNER
            Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (Image img in allImages)
            {
                if (img.gameObject.activeInHierarchy && img.sprite != null)
                {
                    if (IsPointInsideRectTransform(img.rectTransform, mousePos))
                    {
                        string cleanName = img.sprite.name.Replace("_Translated", "").Replace("(Clone)", "").Trim();
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(img.transform);
                        Main.Logger.LogInfo($"[Texture Picker] (Absolute UI) Found: {cleanName} on {img.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        float area = img.rectTransform.rect.width * img.rectTransform.rect.height;
                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = img.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // 2. RAYCAST SCANNER
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    if (TryGetSpriteName(result.gameObject, out string rawName))
                    {
                        string cleanName = rawName.Replace("_Translated", "").Replace("(Clone)", "").Trim();
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(result.gameObject.transform);
                        Main.Logger.LogInfo($"[Texture Picker] (Raycast UI) Found: {cleanName} on {result.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        float area = float.MaxValue;
                        if (result.gameObject.TryGetComponent<RectTransform>(out var rt))
                        {
                            area = rt.rect.width * rt.rect.height;
                        }

                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = result.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // 3. PHYSICS SCANNER
            if (foundCount == 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                RaycastHit[] hits = Physics.RaycastAll(ray);

                foreach (var hit in hits)
                {
                    if (TryGetSpriteName(hit.collider.gameObject, out string rawName))
                    {
                        string cleanName = rawName.Replace("_Translated", "").Replace("(Clone)", "").Trim();
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(hit.collider.transform);
                        Main.Logger.LogInfo($"[Texture Picker] (Physics 3D) Found: {cleanName} on {hit.collider.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        PickedSpriteName = cleanName;
                        PickedPath = path;
                        PickedObjectName = hit.collider.gameObject.name;
                        foundCount++;
                        break;
                    }
                }
            }

            // 4. ABSOLUTE 2D SCANNER
            if (foundCount == 0)
            {
                smallestArea = float.MaxValue;
                SpriteRenderer[] allSRs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                foreach (SpriteRenderer sr in allSRs)
                {
                    if (sr.gameObject.activeInHierarchy && sr.sprite != null && IsObjectUnderMouse(sr.gameObject))
                    {
                        string cleanName = sr.sprite.name.Replace("_Translated", "").Replace("(Clone)", "").Trim();
                        if (cleanName == "BlockSprite" || cleanName == "UIMask") continue;

                        string path = GetPath(sr.transform);
                        Main.Logger.LogInfo($"[Texture Picker] (Absolute 2D) Found: {cleanName} on {sr.gameObject.name}\nPath: {path}\nFile Name: {cleanName}.png");

                        float area = sr.bounds.size.x * sr.bounds.size.y;
                        if (area < smallestArea)
                        {
                            smallestArea = area;
                            PickedSpriteName = cleanName;
                            PickedPath = path;
                            PickedObjectName = sr.gameObject.name;
                        }
                        foundCount++;
                    }
                }
            }

            // Hasil Akhir
            if (foundCount == 0)
            {
                Main.Logger.LogInfo("[Texture Picker] No Sprite Found!");
            }
            else
            {
                Main.Logger.LogInfo("---------------------------------------------");
                Main.Logger.LogInfo($"[Texture Picker] Selected Final : {PickedSpriteName}.png");
                Main.Logger.LogInfo($"[Texture Picker] Object Name    : {PickedObjectName}");
                Main.Logger.LogInfo($"[Texture Picker] Exact Path     : {PickedPath}");
                Main.Logger.LogInfo("---------------------------------------------");
            }
        }

        private static bool TryGetSpriteName(GameObject obj, out string spriteName)
        {
            spriteName = "";
            if (obj == null) return false;

            if (obj.TryGetComponent<Image>(out var img) && img.sprite != null)
            {
                spriteName = img.sprite.name;
                return true;
            }

            if (obj.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
            {
                spriteName = sr.sprite.name;
                return true;
            }

            return false;
        }

        // Scanner untuk komponen Teks UI
        private static void ScanObjectUnderMouse()
        {
            Main.Logger.LogInfo("---------------------------------------------");
            string mode = IsAdvanced ? "Advanced Scanner" : "Absolute Text Scanner";
            Main.Logger.LogInfo($"Path Detector - {mode} Active");
            Main.Logger.LogInfo("---------------------------------------------");

            int foundCount = 0;
            Vector2 mousePos = Input.mousePosition;
            LastScannedSpriteName = "";

            // A. Pengecekan UGUI Text
            Text[] allTexts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (Text t in allTexts)
            {
                if (t.gameObject.activeInHierarchy && IsPointInsideRectTransform(t.rectTransform, mousePos))
                {
                    PrintLog(t.gameObject, t.text, "UI.Text");
                    foundCount++;
                }
            }

            // B. Pengecekan TextMeshPro UI menggunakan HarmonyLib
            System.Type tmpType = HarmonyLib.AccessTools.TypeByName("TMPro.TextMeshProUGUI");
            if (tmpType != null)
            {
                Object[] allTMPs = Object.FindObjectsByType(tmpType, FindObjectsSortMode.None);
                foreach (Object objTmp in allTMPs)
                {
                    Component tmp = (Component)objTmp;
                    if (tmp.gameObject.activeInHierarchy)
                    {
                        RectTransform rt = tmp.GetComponent<RectTransform>();
                        if (rt != null && IsPointInsideRectTransform(rt, mousePos))
                        {
                            var prop = tmpType.GetProperty("text");
                            string txtVal = prop?.GetValue(tmp, null) as string;
                            PrintLog(tmp.gameObject, txtVal, tmpType.Name);
                            foundCount++;
                        }
                    }
                }
            }

            // C. Pengecekan TextMesh 3D
            TextMesh[] allMesh = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach (TextMesh tm in allMesh)
            {
                if (tm.gameObject.activeInHierarchy && IsObjectUnderMouse(tm.gameObject))
                {
                    PrintLog(tm.gameObject, tm.text, "UnityEngine.TextMesh");
                    foundCount++;
                }
            }

            // Fallback: Raycast
            if (foundCount == 0 || IsAdvanced)
            {
                if (EventSystem.current != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (RaycastResult result in results)
                    {
                        if (result.gameObject.GetComponent<Text>() == null && result.gameObject.GetComponent("TMPro.TMP_Text") == null)
                        {
                            if (CheckAndLog(result.gameObject)) foundCount++;
                        }
                    }
                }
            }

            if (foundCount == 0) Main.Logger.LogInfo("No relevant text or component found under mouse.");

            Main.Logger.LogInfo("---------------------------------------------");
            Main.Logger.LogInfo($"Scan Complete. Found {foundCount} object(s).");
            Main.Logger.LogInfo("---------------------------------------------");
        }

        private static bool IsPointInsideRectTransform(RectTransform rectTransform, Vector2 screenPoint)
        {
            Camera cam = null;
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera ?? Camera.main;
            }
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }

        private static bool IsObjectUnderMouse(GameObject obj)
        {
            if (!obj.TryGetComponent<Renderer>(out var r)) return false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Bounds bounds = r.bounds;
            return bounds.IntersectRay(ray);
        }

        private static bool CheckAndLog(GameObject obj)
        {
            if (obj == null) return false;

            string textContent = "N/A";
            string type = "Unknown";
            bool isTarget = false;

            if (obj.TryGetComponent<InputField>(out var inputField))
            {
                textContent = inputField.text;
                type = "UI.InputField";
                isTarget = true;
            }
            else if (obj.GetComponent("TMPro.TMP_InputField") != null)
            {
                Component tmp = obj.GetComponent("TMPro.TMP_InputField");
                var prop = tmp.GetType().GetProperty("text");
                if (prop != null)
                {
                    textContent = prop.GetValue(tmp, null) as string;
                    type = "TMPro.TMP_InputField";
                    isTarget = true;
                }
            }

            if (obj.TryGetComponent<Image>(out var img) && img.sprite != null)
            {
                LastScannedSpriteName = img.sprite.name;
                if (!isTarget) { type = "UI.Image"; textContent = img.sprite.name; isTarget = true; }
            }
            else if (obj.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
            {
                LastScannedSpriteName = sr.sprite.name;
                if (!isTarget) { type = "SpriteRenderer"; textContent = sr.sprite.name; isTarget = true; }
            }

            if (!string.IsNullOrEmpty(LastScannedSpriteName))
            {
                LastScannedSpriteName = LastScannedSpriteName.Replace("_Translated", "").Replace("(Clone)", "").Trim();
            }

            if (isTarget || (IsAdvanced && obj.GetComponent<RectTransform>() != null))
            {
                PrintLog(obj, textContent, type);
                return true;
            }

            return false;
        }

        private static void PrintLog(GameObject obj, string text, string type)
        {
            string path = GetPath(obj.transform);
            string jsonKey = EscapeForJson(text);

            if (IsAdvanced)
            {
                string posInfo = "";
                if (obj.TryGetComponent<RectTransform>(out var rect))
                {
                    posInfo += $"\nPos (X, Y)  : {rect.anchoredPosition.x:F1}, {rect.anchoredPosition.y:F1}";
                    posInfo += $"\nSize (W, H) : {rect.rect.width:F0}, {rect.rect.height:F0}";
                    posInfo += $"\nPivot       : {rect.pivot}";
                }

                string layoutInfo = "None";
                if (obj.transform.parent != null)
                {
                    if (obj.transform.parent.GetComponent<VerticalLayoutGroup>()) layoutInfo = "VerticalLayoutGroup";
                    else if (obj.transform.parent.GetComponent<HorizontalLayoutGroup>()) layoutInfo = "HorizontalLayoutGroup";
                    else if (obj.transform.parent.GetComponent<GridLayoutGroup>()) layoutInfo = "GridLayoutGroup";
                }

                string spriteInfo = string.IsNullOrEmpty(LastScannedSpriteName) ? "" : $"\nSprite Name : {LastScannedSpriteName}\nFile Name   : {LastScannedSpriteName}.png";

                Main.Logger.LogInfo($"Text       : {text}\n" +
                                    $"JSON Key   : {jsonKey}\n" +
                                    $"Path       : {path}\n" +
                                    $"Type       : {type}\n" +
                                    $"Parent Lay : {layoutInfo}" +
                                    spriteInfo +
                                    posInfo);
            }
            else
            {
                Main.Logger.LogInfo($"Text : {text}\nJSON Key : {jsonKey}\nPath : {path}\nType : {type}");
                if (!string.IsNullOrEmpty(LastScannedSpriteName) && (type.Contains("Image") || type.Contains("Sprite")))
                    Main.Logger.LogInfo($"Sprite Name: {LastScannedSpriteName}\nFile Name: {LastScannedSpriteName}.png");
            }
        }

        private static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static string GetPath(Transform current)
        {
            if (current.parent == null)
                return "/" + current.name;
            return GetPath(current.parent) + "/" + current.name;
        }
    }
}