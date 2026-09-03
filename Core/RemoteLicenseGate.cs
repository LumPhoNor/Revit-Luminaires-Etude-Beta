using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Vérifie à distance si l'usage du plugin est autorisé, sans nécessiter de nouvelle
    /// version pour couper l'accès ou passer en payant : le statut vit dans un fichier JSON
    /// hébergé sur le site (modifiable à tout moment), le plugin le consulte périodiquement.
    /// </summary>
    public static class RemoteLicenseGate
    {
        private const string StatusUrl = "https://getskylightning.com/status/skylightning-status.json";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan RecheckInterval = TimeSpan.FromHours(4);
        private static readonly TimeSpan OfflineGrace = TimeSpan.FromDays(14);

        private static readonly object _lock = new object();
        private static GateResult _cachedResult;
        private static DateTime _lastCheckUtc = DateTime.MinValue;

        public class GateResult
        {
            public bool Allowed { get; set; }
            public string Message { get; set; }
        }

        private class RemoteStatus
        {
            public string status { get; set; }
            public string message { get; set; }
        }

        private class LocalCache
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public DateTime LastSuccessUtc { get; set; }
        }

        /// <summary>
        /// À appeler en tête des commandes qui doivent respecter le statut distant
        /// (typiquement le Calcul). Revérifie via le réseau au plus toutes les
        /// <see cref="RecheckInterval"/>, sinon renvoie le dernier résultat en mémoire.
        /// </summary>
        public static GateResult EnsureAccessAllowed()
        {
            lock (_lock)
            {
                if (_cachedResult != null && DateTime.UtcNow - _lastCheckUtc < RecheckInterval)
                    return _cachedResult;

                _cachedResult = CheckRemote();
                _lastCheckUtc = DateTime.UtcNow;
                return _cachedResult;
            }
        }

        private static GateResult CheckRemote()
        {
            try
            {
                using (var client = new HttpClient { Timeout = RequestTimeout })
                {
                    string url = StatusUrl + "?ts=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string json = client.GetStringAsync(url).GetAwaiter().GetResult();
                    var remote = JsonConvert.DeserializeObject<RemoteStatus>(json);

                    bool allowed = !string.Equals(remote?.status, "blocked", StringComparison.OrdinalIgnoreCase);
                    SaveCache(new LocalCache
                    {
                        Status = remote?.status ?? "active",
                        Message = remote?.message,
                        LastSuccessUtc = DateTime.UtcNow
                    });

                    Logger.Info("RemoteLicenseGate", $"Statut distant : {remote?.status ?? "active"}");
                    return new GateResult { Allowed = allowed, Message = remote?.message };
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("RemoteLicenseGate", $"Vérification distante impossible ({ex.Message}), repli sur le cache local");
                return CheckOfflineGrace();
            }
        }

        private static GateResult CheckOfflineGrace()
        {
            var cache = LoadCache();
            if (cache == null)
            {
                // Jamais réussi à joindre le serveur : on laisse passer plutôt que de
                // bloquer un beta-testeur au tout premier lancement sans connexion.
                return new GateResult { Allowed = true, Message = null };
            }

            bool withinGrace = DateTime.UtcNow - cache.LastSuccessUtc < OfflineGrace;
            bool wasBlocked  = string.Equals(cache.Status, "blocked", StringComparison.OrdinalIgnoreCase);

            if (wasBlocked)
                return new GateResult { Allowed = false, Message = cache.Message };

            if (withinGrace)
                return new GateResult { Allowed = true, Message = null };

            return new GateResult
            {
                Allowed = false,
                Message = "Connexion internet requise pour revalider l'accès à Skylightning " +
                           "(dernière vérification il y a plus de 14 jours)."
            };
        }

        private static string CacheFilePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Skylightning");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "license_cache.json");
        }

        private static void SaveCache(LocalCache cache)
        {
            try { File.WriteAllText(CacheFilePath(), JsonConvert.SerializeObject(cache)); }
            catch (Exception ex) { Logger.Warning("RemoteLicenseGate", $"Écriture cache licence échouée : {ex.Message}"); }
        }

        private static LocalCache LoadCache()
        {
            try
            {
                string path = CacheFilePath();
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<LocalCache>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Logger.Warning("RemoteLicenseGate", $"Lecture cache licence échouée : {ex.Message}");
                return null;
            }
        }
    }
}
