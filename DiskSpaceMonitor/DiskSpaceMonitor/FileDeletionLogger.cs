using System.IO;
using System.Text;

namespace DiskSpaceMonitor
{
    public class FileDeletionLogger
    {
        private readonly string _logFilePath;

        public FileDeletionLogger(string logFilePath = "deletion_log.txt")
        {
            _logFilePath = logFilePath;

            if (!File.Exists(_logFilePath))
                File.Create(_logFilePath).Close();
        }

        public void LogSuccess(string filePath, string user = null)
        {
            var logEntry = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] УДАЛЕНО: {filePath}";
            if (!string.IsNullOrEmpty(user))
            {
                logEntry += $" | Пользователь: {user}";
            }
            WriteToLog(logEntry);
        }

        public void LogFailure(string filePath, string errorMessage, string user = null)
        {
            var logEntry = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] ОШИБКА УДАЛЕНИЯ: {filePath} | Причина: {errorMessage}";
            if (!string.IsNullOrEmpty(user))
            {
                logEntry += $" | Пользователь: {user}";
            }
            WriteToLog(logEntry);
        }

        private void WriteToLog(string message)
        {
            try
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА ЛОГИРОВАНИЯ: {ex.Message}");
            }
        }
    }
}
