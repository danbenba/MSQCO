using System;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace MSQCO
{
    class Program
    {
        private const string ConfigPath = "config.json";
        private const string LogPath    = "console.log";

        static void Main(string[] args)
        {
            // --reset efface config.json et console.log
            if (args.Length > 0 && args[0].Equals("--reset", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
                if (File.Exists(LogPath))    File.Delete(LogPath);
                Console.WriteLine("Configuration et journal supprimés.");
                return;
            }

            // Charge ou crée la config (mode JDBC ou classique)
            var config = LoadOrCreateConfig();

            // Prépare le log
            using var logWriter = new StreamWriter(LogPath, append: true);
            logWriter.WriteLine($"-- Nouvelle session démarrée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            Console.Clear();
            WriteHeader();

            // Construit la chaîne de connexion ADO.NET
            string connString = config.Mode == "jdbc"
                ? BuildConnStringFromJdbc(config.JDBC)
                : $"Server={config.Server};Port={config.Port};Database={config.Database};Uid={config.User};Pwd={config.Password};";

            using var connection = new MySqlConnection(connString);
            try
            {
                connection.Open();
                WriteSuccess("Connecté à la base de données MySQL.");
                logWriter.WriteLine("[INFO] Connexion réussie.");
            }
            catch (Exception ex)
            {
                WriteError($"Erreur de connexion : {ex.Message}");
                logWriter.WriteLine($"[ERROR] Échec connexion : {ex.Message}");
                PromptExit();
                return;
            }

            // Boucle de saisie SQL
            WriteInfo("Entrez vos commandes SQL (tapez 'exit' pour quitter) :");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");
                Console.ResetColor();

                var commandText = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(commandText))
                    continue;

                logWriter.WriteLine($"[COMMAND] {DateTime.Now:HH:mm:ss} {commandText}");

                if (commandText.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                using var command = new MySqlCommand(commandText, connection);
                try
                {
                    if (commandText.TrimStart().StartsWith("select", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = command.ExecuteReader();
                        // En-têtes
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(reader.GetName(i));
                            Console.ResetColor();
                            Console.Write("\t");
                        }
                        Console.WriteLine();
                        // Lignes
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write(reader.GetValue(i)?.ToString());
                                Console.Write("\t");
                            }
                            Console.WriteLine();
                        }
                        logWriter.WriteLine("[INFO] Requête SELECT exécutée.");
                    }
                    else
                    {
                        int affected = command.ExecuteNonQuery();
                        WriteSuccess($"Commande exécutée, {affected} ligne(s) affectée(s).");
                        logWriter.WriteLine($"[INFO] Non-SELECT exécuté, {affected} ligne(s) affectée(s).");
                    }
                }
                catch (Exception ex)
                {
                    WriteError($"Erreur SQL : {ex.Message}");
                    logWriter.WriteLine($"[ERROR] Erreur SQL : {ex.Message}");
                }
            }

            connection.Close();
            WriteInfo("Déconnecté.");
            logWriter.WriteLine("[INFO] Déconnexion.");
            logWriter.WriteLine();
            PromptExit();
        }

        private static Config LoadOrCreateConfig()
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<Config>(json)!;
            }

            Console.WriteLine("--- Configuration de la connexion MySQL ---");
            Console.WriteLine("Choisissez le mode de saisie :");
            Console.WriteLine("  1) JDBC URL complète");
            Console.WriteLine("  2) Mode classique (hôte, base, utilisateur, mot de passe)");
            Console.Write("Votre choix (1 ou 2) : ");

            string? choice;
            do
            {
                choice = Console.ReadLine()?.Trim();
            } while (choice != "1" && choice != "2");

            var config = new Config
            {
                Mode = choice == "1" ? "jdbc" : "classic"
            };

            if (config.Mode == "jdbc")
            {
                Console.Write("JDBC URL : ");
                config.JDBC = Console.ReadLine()!;
            }
            else
            {
                Console.Write("Serveur (ou IP) : ");
                config.Server = Console.ReadLine()!;
                Console.Write("Port [3306] : ");
                var portInput = Console.ReadLine();
                config.Port = int.TryParse(portInput, out var p) ? p : 3306;
                Console.Write("Base de données : ");
                config.Database = Console.ReadLine()!;
                Console.Write("Utilisateur : ");
                config.User = Console.ReadLine()!;
                Console.Write("Mot de passe : ");
                config.Password = ReadPassword();
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, options));
            Console.WriteLine("\nConfiguration enregistrée dans config.json.\n");
            return config;
        }

        private static string BuildConnStringFromJdbc(string jdbcUrl)
        {
            // Enlève "jdbc:" si présent
            var url = jdbcUrl.StartsWith("jdbc:") ? jdbcUrl.Substring(5) : jdbcUrl;
            var uri = new Uri(url);
            var parts = uri.UserInfo.Split(':', 2);
            string user     = parts[0];
            string password = parts.Length > 1 ? parts[1] : "";
            string host     = uri.Host;
            int    port     = uri.Port;
            string database = uri.AbsolutePath.TrimStart('/');

            return $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};";
        }

        private static string ReadPassword()
        {
            string pwd = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (!char.IsControl(key.KeyChar))
                {
                    pwd += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd = pwd[0..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pwd;
        }

        static void WriteHeader()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("=============================================");
            Console.WriteLine(" MySql Connection Tool - Created by danbenba ");
            Console.WriteLine("            Copyright (c) 2025 Dany          ");
            Console.WriteLine("=============================================");
            Console.ResetColor();
        }
        static void WriteInfo(string m)    { Console.ForegroundColor = ConsoleColor.White;   Console.WriteLine($"[INFO]    {m}"); Console.ResetColor(); }
        static void WriteSuccess(string m) { Console.ForegroundColor = ConsoleColor.Green;   Console.WriteLine($"[SUCCESS] {m}"); Console.ResetColor(); }
        static void WriteError(string m)   { Console.ForegroundColor = ConsoleColor.Red;     Console.WriteLine($"[ERROR]   {m}"); Console.ResetColor(); }
        static void PromptExit()           { Console.ForegroundColor = ConsoleColor.Gray;    Console.WriteLine("Appuyez sur une touche pour quitter…"); Console.ResetColor(); Console.ReadKey(true); }
    }

    public class Config
    {
        public string Mode      { get; set; } = "jdbc";      // "jdbc" ou "classic"
        public string JDBC      { get; set; } = string.Empty;
        public string Server    { get; set; } = string.Empty;
        public int    Port      { get; set; } = 3306;
        public string Database  { get; set; } = string.Empty;
        public string User      { get; set; } = string.Empty;
        public string Password  { get; set; } = string.Empty;
    }
}
