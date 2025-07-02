using UnityEngine;
using UnityEditor;
using System.Net;
using System.Threading;
using System.IO;
using System;
using UnityEngine.Networking;

[System.Serializable]
public class SongInfo
{
    public string title;
    public string artist;
    public string thumbnailUrl;
    public string currentTime;
    public string totalDuration;
}

public class YtMusicWindow : EditorWindow
{
    private static HttpListener listener;
    private static Thread serverThread;
    private static SongInfo currentSong;
    private static Texture2D currentThumbnail;
    private static string lastThumbnailUrl;
    private static bool dataUpdated = false;
    private static UnityWebRequest webRequest;

    [MenuItem("Window/YouTube Music")]
    public static void ShowWindow()
    {
        GetWindow<YtMusicWindow>("YouTube Music");
    }

    void OnEnable()
    {
        if (serverThread == null || !serverThread.IsAlive)
        {
            serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();
        }
        EditorApplication.update += OnUpdate;
    }

    void OnDisable()
    {
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
        }
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Abort();
        }
        EditorApplication.update -= OnUpdate;
    }
    
    private void OnUpdate()
    {
        if (dataUpdated)
        {
            dataUpdated = false;
            Repaint();
        }

        if (webRequest != null && webRequest.isDone)
        {
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                currentThumbnail = DownloadHandlerTexture.GetContent(webRequest);
                Repaint();
            }
            else
            {
                Debug.LogError("Thumbnail download failed: " + webRequest.error);
            }
            webRequest.Dispose();
            webRequest = null;
        }
    }

    void OnGUI()
    {
        if (currentSong == null)
        {
            EditorGUILayout.LabelField("Waiting for data from Firefox...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        GUILayout.Space(10);
        Rect artworkRect = EditorGUILayout.GetControlRect(GUILayout.Height(Mathf.Min(position.width * 0.8f, 400f)));

        if (currentThumbnail != null)
        {
            GUI.DrawTexture(artworkRect, currentThumbnail, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.LabelField(artworkRect, "Loading thumbnail...", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField(currentSong.title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true });
        EditorGUILayout.LabelField(currentSong.artist, new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true });
        GUILayout.Space(5);

        // Calculate progress percentage
        float currentTimeSec = ParseTimeToSeconds(currentSong.currentTime);
        float totalTimeSec = ParseTimeToSeconds(currentSong.totalDuration);
        float progress = (totalTimeSec > 0) ? currentTimeSec / totalTimeSec : 0f;

        // Draw the progress bar
        Rect r = EditorGUILayout.GetControlRect();

        // Save the original GUI color
        Color originalColor = GUI.color;
        // Set the color for the following GUI elements to white
        GUI.color = Color.white;

        // Draw the progress bar (it will now be white)
        EditorGUI.ProgressBar(r, progress, $"{currentSong.currentTime} / {currentSong.totalDuration}");

        // Restore the original color so it doesn't affect other UI
        GUI.color = originalColor;
    }

    private float ParseTimeToSeconds(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return 0;
        var parts = timeStr.Split(':');
        if (parts.Length != 2) return 0;
        if (int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
        {
            return (minutes * 60) + seconds;
        }
        return 0;
    }

    private static void StartServer()
    {
        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Debug.Log("Server started. Listening on http://localhost:8080/");

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                ProcessRequest(context);
            }
        }
        catch (ThreadAbortException)
        {
            Debug.Log("Server thread aborted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Server Error: {e.Message}");
        }
    }

    private static void ProcessRequest(HttpListenerContext context)
    {
        // Handle CORS (Cross-Origin Resource Sharing)
        context.Response.Headers.Set("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Set("Access-Control-Allow-Methods", "POST, OPTIONS");
        context.Response.Headers.Set("Access-Control-Allow-Headers", "Content-Type");

        if (context.Request.HttpMethod == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            context.Response.Close();
            return;
        }

        // Process the actual POST request
        string json;
        using (StreamReader reader = new StreamReader(context.Request.InputStream))
        {
            json = reader.ReadToEnd();
        }

        SongInfo receivedSong = JsonUtility.FromJson<SongInfo>(json);

        if (receivedSong == null)
        {
            Debug.LogWarning($"Received invalid song data. JSON: {json}");
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }
        
        currentSong = receivedSong;

        if (lastThumbnailUrl != currentSong.thumbnailUrl)
        {
            lastThumbnailUrl = currentSong.thumbnailUrl;
            string urlToDownload = currentSong.thumbnailUrl;

            EditorApplication.delayCall += () =>
            {
                if (webRequest != null && !webRequest.isDone)
                {
                    webRequest.Abort();
                }
                webRequest = UnityWebRequestTexture.GetTexture(urlToDownload);
                webRequest.SendWebRequest();
            };
        }

        dataUpdated = true;
        context.Response.StatusCode = 200;
        context.Response.Close();
    }
}