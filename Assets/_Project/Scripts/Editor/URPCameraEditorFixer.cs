#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

/// <summary>
/// Unity'nin URP paketindeki bilinen bir Editor bug'ini susturur.
/// Sahne gecisleri sirasinda Camera objesi yok edildiginde URP'nin kendi Editor scripti
/// "SerializedObjectNotCreatableException: Object at index 0 is null" hatasi firlatir.
/// Bu hata SADECE Editor'de olur, build'de olmaz ve oyunu etkilemez.
/// 
/// Bu script Unity'nin log handler'ini ozel bir filtre ile sarmalayarak
/// o spesifik hatayi konsola ULASMADAN ONCE yakalar ve susturur.
/// </summary>
[InitializeOnLoad]
public static class URPCameraEditorFixer
{
    static URPCameraEditorFixer()
    {
        // Unity'nin default log handler'ini bizim filtreli handler ile sarmala
        if (!(Debug.unityLogger.logHandler is FilteredLogHandler))
        {
            Debug.unityLogger.logHandler = new FilteredLogHandler(Debug.unityLogger.logHandler);
        }
    }

    /// <summary>
    /// Unity'nin ILogHandler arayuzunu implement eden ozel filtre.
    /// Tum loglari normal sekilde iletir, SADECE URP Camera Editor hatasini yutar.
    /// </summary>
    private class FilteredLogHandler : ILogHandler
    {
        private readonly ILogHandler defaultHandler;

        public FilteredLogHandler(ILogHandler defaultHandler)
        {
            this.defaultHandler = defaultHandler;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            // Normal log mesajlarini oldugu gibi ilet
            defaultHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            // URP Camera Editor'un bilinen bug hatasini sustur
            if (exception != null && 
                exception.GetType().Name.Contains("SerializedObjectNotCreatable"))
            {
                return; // Konsola yazma, yut
            }

            // Diger tum hatalari normal sekilde ilet
            defaultHandler.LogException(exception, context);
        }
    }
}
#endif
