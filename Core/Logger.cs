using System;
using System.IO;
using System.Text;
using System.Threading;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Système de logging centralisé avec fichiers horodatés
    /// Fichiers : Logs/RevitLightingPlugin_YYYYMMDD_HHmmss.log
    /// </summary>
    public static class Logger
    {
        private static string _logFilePath;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// Niveaux de log
        /// </summary>
        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR,
            CRITICAL
        }

        /// <summary>
        /// Niveau minimum de log à enregistrer (défaut: DEBUG = tout)
        /// </summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.DEBUG;

        /// <summary>
        /// Initialise le système de logging
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            lock (_lockObject)
            {
                if (_isInitialized) return;

                try
                {
                    // Créer le dossier Logs à la racine du projet
                    string projectRoot = GetProjectRoot();
                    string logsFolder = Path.Combine(projectRoot, "Logs");

                    if (!Directory.Exists(logsFolder))
                    {
                        Directory.CreateDirectory(logsFolder);
                    }

                    // Nom du fichier avec date et heure
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"RevitLightingPlugin_{timestamp}.log";
                    _logFilePath = Path.Combine(logsFolder, fileName);

                    // Créer le fichier avec en-tête
                    WriteHeader();

                    _isInitialized = true;

                    Info("Logger", "Système de logging initialisé");
                    Info("Logger", $"Fichier de log : {_logFilePath}");
                }
                catch (Exception ex)
                {
                    // Fallback : écrire dans TEMP si erreur
                    string tempFile = Path.Combine(Path.GetTempPath(), $"RevitLightingPlugin_Fallback_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    _logFilePath = tempFile;
                    _isInitialized = true;
                    Error("Logger", $"Erreur initialisation logging, fallback vers TEMP : {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Trouve la racine du projet (remonte depuis le dossier de l'assembly)
        /// </summary>
        private static string GetProjectRoot()
        {
            try
            {
                // Chemin de l'assembly en cours
                string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string assemblyDir = Path.GetDirectoryName(assemblyPath);

                // Remonter pour trouver la racine du projet (cherche le .csproj)
                DirectoryInfo dir = new DirectoryInfo(assemblyDir);
                while (dir != null && dir.Parent != null)
                {
                    // Chercher un fichier .csproj
                    var csprojFiles = dir.GetFiles("*.csproj");
                    if (csprojFiles.Length > 0)
                    {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }

                // Si pas trouvé, utiliser le dossier Documents
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Projets Plugin", "RevitLightingPlugin"
                );
            }
            catch
            {
                // Fallback ultime
                return Path.GetTempPath();
            }
        }

        /// <summary>
        /// Écrit l'en-tête du fichier de log
        /// </summary>
        private static void WriteHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                    REVIT LIGHTING PLUGIN - LOG FILE                    ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine($"Session démarrée : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Version : 2.0");
            sb.AppendLine($"Machine : {Environment.MachineName}");
            sb.AppendLine($"Utilisateur : {Environment.UserName}");
            sb.AppendLine($"OS : {Environment.OSVersion}");
            sb.AppendLine($".NET Framework : {Environment.Version}");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            File.WriteAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Écrit une ligne de log
        /// </summary>
        private static void Write(LogLevel level, string category, string message, Exception exception = null)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Vérifier le niveau minimum
            if (level < MinimumLevel) return;

            lock (_lockObject)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Timestamp
                    sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");

                    // Niveau avec couleur emoji
                    string levelIcon;
                    switch (level)
                    {
                        case LogLevel.DEBUG:
                            levelIcon = "🔍 DEBUG   ";
                            break;
                        case LogLevel.INFO:
                            levelIcon = "ℹ️  INFO    ";
                            break;
                        case LogLevel.WARNING:
                            levelIcon = "⚠️  WARNING ";
                            break;
                        case LogLevel.ERROR:
                            levelIcon = "❌ ERROR   ";
                            break;
                        case LogLevel.CRITICAL:
                            levelIcon = "🔥 CRITICAL";
                            break;
                        default:
                            levelIcon = "   UNKNOWN ";
                            break;
                    }
                    sb.Append($"{levelIcon} ");

                    // Catégorie
                    sb.Append($"[{category,-20}] ");

                    // Thread ID
                    sb.Append($"[T{Thread.CurrentThread.ManagedThreadId:D3}] ");

                    // Message
                    sb.Append(message);

                    // Exception si présente
                    if (exception != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"    Exception: {exception.GetType().Name}");
                        sb.AppendLine($"    Message: {exception.Message}");
                        sb.AppendLine($"    StackTrace: {exception.StackTrace}");

                        if (exception.InnerException != null)
                        {
                            sb.AppendLine($"    Inner Exception: {exception.InnerException.Message}");
                        }
                    }

                    // Écrire dans le fichier
                    File.AppendAllText(_logFilePath, sb.ToString() + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Si erreur d'écriture, tenter dans TEMP
                    try
                    {
                        string fallbackFile = Path.Combine(Path.GetTempPath(), $"RevitLightingPlugin_Emergency_{DateTime.Now:yyyyMMdd}.log");
                        File.AppendAllText(fallbackFile, $"[EMERGENCY LOG] {DateTime.Now}: {message} | Original error: {ex.Message}\n");
                    }
                    catch
                    {
                        // Silence - ne rien faire si vraiment impossible d'écrire
                    }
                }
            }
        }

        #region Méthodes publiques de logging

        /// <summary>
        /// Log DEBUG - Informations détaillées pour le débogage
        /// </summary>
        public static void Debug(string category, string message)
        {
            Write(LogLevel.DEBUG, category, message);
        }

        /// <summary>
        /// Log INFO - Informations générales
        /// </summary>
        public static void Info(string category, string message)
        {
            Write(LogLevel.INFO, category, message);
        }

        /// <summary>
        /// Log WARNING - Avertissements
        /// </summary>
        public static void Warning(string category, string message)
        {
            Write(LogLevel.WARNING, category, message);
        }

        /// <summary>
        /// Log ERROR - Erreurs
        /// </summary>
        public static void Error(string category, string message, Exception exception = null)
        {
            Write(LogLevel.ERROR, category, message, exception);
        }

        /// <summary>
        /// Log CRITICAL - Erreurs critiques
        /// </summary>
        public static void Critical(string category, string message, Exception exception = null)
        {
            Write(LogLevel.CRITICAL, category, message, exception);
        }

        /// <summary>
        /// Log une séparation visuelle
        /// </summary>
        public static void Separator(string title = null)
        {
            if (!_isInitialized) Initialize();

            lock (_lockObject)
            {
                string separator = title != null
                    ? $"════════════════════ {title} ════════════════════"
                    : "════════════════════════════════════════════════════════════════════════";

                File.AppendAllText(_logFilePath, separator + Environment.NewLine, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Log l'entrée dans une méthode
        /// </summary>
        public static void EnterMethod(string className, string methodName, params object[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append($"▶️ ENTER {className}.{methodName}(");

            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(parameters[i]?.ToString() ?? "null");
                }
            }

            sb.Append(")");

            Debug("MethodTrace", sb.ToString());
        }

        /// <summary>
        /// Log la sortie d'une méthode
        /// </summary>
        public static void ExitMethod(string className, string methodName, object returnValue = null)
        {
            string message = returnValue != null
                ? $"◀️ EXIT {className}.{methodName}() => {returnValue}"
                : $"◀️ EXIT {className}.{methodName}()";

            Debug("MethodTrace", message);
        }

        /// <summary>
        /// Log les performances d'une opération
        /// </summary>
        public static void Performance(string operation, TimeSpan duration)
        {
            Info("Performance", $"{operation} completed in {duration.TotalMilliseconds:F2} ms");
        }

        /// <summary>
        /// Ferme le fichier de log avec un footer
        /// </summary>
        public static void Close()
        {
            if (!_isInitialized) return;

            lock (_lockObject)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    sb.AppendLine("════════════════════════════════════════════════════════════════════════");
                    sb.AppendLine($"Session terminée : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");

                    File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // Silence
                }
            }
        }

        /// <summary>
        /// Obtient le chemin du fichier de log actuel
        /// </summary>
        public static string GetLogFilePath()
        {
            if (!_isInitialized) Initialize();
            return _logFilePath;
        }

        #endregion
    }
}
