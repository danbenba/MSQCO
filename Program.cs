using System;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace MySqlConsoleClient
{
    class Program
    {
        private const string ConfigPath = "config.json";
        private const string LogPath = "console.log";

        static void Main(string[] args)
        {
            // Gestion du reset
            if (args.Length > 0 && args[0].Equals("--reset", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
                if (File.Exists(LogPath)) File.Delete(LogPath);
                Console.WriteLine("Configuration et journal supprimés.");
                return;
            }

            // Charge ou crée la configuration
            var config = LoadOrCreateConfig();

            // Prépare le log de session
            using var logWriter = new StreamWriter(LogPath, append: true);
            logWriter.WriteLine($"-- Nouvelle session démarrée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            Console.Clear();
            WriteHeader();

            string connString = $"Server={config.Server};Database={config.Database};User ID={config.User};Password={config.Password};";
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
                        // Afficher les noms de colonnes
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(reader.GetName(i));
                            Console.ResetColor();
                            Console.Write("\t");
                        }
                        Console.WriteLine();

                        // Afficher les résultats
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
                        WriteSuccess($"Commande exécutée, {affected} ligne(s) affectée(s). ");
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

            // Nouvelle configuration
            Console.WriteLine("--- Configuration de la connexion MySQL ---");
            Console.Write("Serveur: "); var server = Console.ReadLine()!;
            Console.Write("Base de données: "); var database = Console.ReadLine()!;
            Console.Write("Utilisateur: "); var user = Console.ReadLine()!;
            Console.Write("Mot de passe: "); var password = ReadPassword();

            var config = new Config { Server = server, Database = database, User = user, Password = password };
            var jsonOut = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, jsonOut);
            Console.WriteLine("Configuration enregistrée dans config.json.\n");
            return config;
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

        static void WriteInfo(string message) { Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"[INFO] {message}"); Console.ResetColor(); }
        static void WriteSuccess(string message) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"[SUCCESS] {message}"); Console.ResetColor(); }
        static void WriteError(string message) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"[ERROR] {message}"); Console.ResetColor(); }
        static void PromptExit() { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine("Appuyez sur une touche pour quitter..."); Console.ResetColor(); Console.ReadKey(true); }
    }

    public class Config
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
