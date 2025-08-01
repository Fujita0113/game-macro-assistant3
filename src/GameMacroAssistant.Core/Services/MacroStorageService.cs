using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IMacroStorageService
{
    Task<Macro?> LoadMacroAsync(string filePath, string? passphrase = null);
    Task SaveMacroAsync(Macro macro, string filePath, string? passphrase = null);
    Task<List<string>> GetMacroFilesAsync(string directory);
    Task<bool> ValidateMacroFileAsync(string filePath);
    Task<string> ExportMacroAsync(Macro macro, string format = "json");
}

public class MacroStorageService : IMacroStorageService
{
    private readonly ILogger _logger;
    private const string FILE_EXTENSION = ".gma.json";
    private const int MAX_PASSPHRASE_ATTEMPTS = 3;
    
    public MacroStorageService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task<Macro?> LoadMacroAsync(string filePath, string? passphrase = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("Macro file not found: {FilePath}", filePath);
                return null;
            }
            
            var jsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            
            // Try to deserialize to check if encrypted
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;
            
            var isEncrypted = root.TryGetProperty("isEncrypted", out var encryptedProp) && 
                             encryptedProp.GetBoolean();
            
            if (isEncrypted)
            {
                if (string.IsNullOrEmpty(passphrase))
                {
                    throw new UnauthorizedAccessException("Passphrase required for encrypted macro");
                }
                
                var attempts = 0;
                while (attempts < MAX_PASSPHRASE_ATTEMPTS)
                {
                    try
                    {
                        jsonContent = DecryptMacroContent(jsonContent, passphrase);
                        break;
                    }
                    catch (CryptographicException)
                    {
                        attempts++;
                        if (attempts >= MAX_PASSPHRASE_ATTEMPTS)
                        {
                            _logger.LogError("Maximum passphrase attempts exceeded for file: {FilePath}", filePath);
                            throw new UnauthorizedAccessException("Invalid passphrase - maximum attempts exceeded");
                        }
                        
                        _logger.LogWarning("Invalid passphrase attempt {Attempt}/{MaxAttempts}", attempts, MAX_PASSPHRASE_ATTEMPTS);
                        throw new UnauthorizedAccessException($"Invalid passphrase (attempt {attempts}/{MAX_PASSPHRASE_ATTEMPTS})");
                    }
                }
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var macro = JsonSerializer.Deserialize<Macro>(jsonContent, options);
            
            if (macro == null)
            {
                _logger.LogError("Failed to deserialize macro from file: {FilePath}", filePath);
                return null;
            }
            
            // Validate schema version
            if (macro.SchemaVersion != "1.0")
            {
                _logger.LogWarning("Unsupported schema version {Version} in file: {FilePath}", 
                    macro.SchemaVersion, filePath);
            }
            
            _logger.LogInformation("Successfully loaded macro: {MacroName} from {FilePath}", 
                macro.Name, filePath);
            
            return macro;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load macro from file: {FilePath}", filePath);
            return null;
        }
    }
    
    public async Task SaveMacroAsync(Macro macro, string filePath, string? passphrase = null)
    {
        try
        {
            // Validate passphrase requirements (R-018)
            if (!string.IsNullOrEmpty(passphrase))
            {
                if (passphrase.Length < 8)
                {
                    throw new ArgumentException("Passphrase must be at least 8 characters long");
                }
                
                macro.IsEncrypted = true;
                macro.PassphraseHash = HashPassphrase(passphrase);
            }
            else
            {
                macro.IsEncrypted = false;
                macro.PassphraseHash = null;
            }
            
            // Ensure proper file extension
            if (!filePath.EndsWith(FILE_EXTENSION))
            {
                filePath = Path.ChangeExtension(filePath, FILE_EXTENSION.TrimStart('.'));
            }
            
            // Update timestamps
            macro.ModifiedAt = DateTime.UtcNow;
            if (macro.CreatedAt == default)
            {
                macro.CreatedAt = DateTime.UtcNow;
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var jsonContent = JsonSerializer.Serialize(macro, options);
            
            // Encrypt if passphrase provided
            if (!string.IsNullOrEmpty(passphrase))
            {
                jsonContent = EncryptMacroContent(jsonContent, passphrase);
            }
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(filePath, jsonContent, Encoding.UTF8);
            
            _logger.LogInformation("Successfully saved macro: {MacroName} to {FilePath}", 
                macro.Name, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save macro to file: {FilePath}", filePath);
            throw;
        }
    }
    
    public async Task<List<string>> GetMacroFilesAsync(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }
            
            var files = Directory.GetFiles(directory, $"*{FILE_EXTENSION}", SearchOption.AllDirectories);
            var validFiles = new List<string>();
            
            foreach (var file in files)
            {
                if (await ValidateMacroFileAsync(file))
                {
                    validFiles.Add(file);
                }
            }
            
            return validFiles.OrderBy(Path.GetFileName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get macro files from directory: {Directory}", directory);
            return new List<string>();
        }
    }
    
    public async Task<bool> ValidateMacroFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(FILE_EXTENSION))
            {
                return false;
            }
            
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            // Check required properties
            return root.TryGetProperty("id", out _) &&
                   root.TryGetProperty("name", out _) &&
                   root.TryGetProperty("schemaVersion", out _) &&
                   root.TryGetProperty("steps", out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid macro file: {FilePath}", filePath);
            return false;
        }
    }
    
    public async Task<string> ExportMacroAsync(Macro macro, string format = "json")
    {
        return await Task.Run(() =>
        {
            switch (format.ToLowerInvariant())
            {
                case "json":
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Serialize(macro, options);
                
                case "readable":
                    return GenerateReadableFormat(macro);
                
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        });
    }
    
    private string EncryptMacroContent(string content, string passphrase)
    {
        using var aes = Aes.Create();
        var key = DeriveKeyFromPassphrase(passphrase, aes.KeySize / 8);
        var iv = new byte[aes.BlockSize / 8];
        RandomNumberGenerator.Fill(iv);
        
        aes.Key = key;
        aes.IV = iv;
        
        using var encryptor = aes.CreateEncryptor();
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var encryptedBytes = encryptor.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
        
        // Combine IV and encrypted data
        var result = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
        
        return Convert.ToBase64String(result);
    }
    
    private string DecryptMacroContent(string encryptedContent, string passphrase)
    {
        // Extract the encrypted content from JSON
        using var document = JsonDocument.Parse(encryptedContent);
        var root = document.RootElement;
        
        if (!root.TryGetProperty("encryptedData", out var encryptedDataProp))
        {
            throw new InvalidDataException("Missing encrypted data in file");
        }
        
        var encryptedData = Convert.FromBase64String(encryptedDataProp.GetString()!);
        
        using var aes = Aes.Create();
        var keySize = aes.KeySize / 8;
        var ivSize = aes.BlockSize / 8;
        
        if (encryptedData.Length < ivSize)
        {
            throw new InvalidDataException("Invalid encrypted data format");
        }
        
        var iv = new byte[ivSize];
        var cipherText = new byte[encryptedData.Length - ivSize];
        
        Buffer.BlockCopy(encryptedData, 0, iv, 0, ivSize);
        Buffer.BlockCopy(encryptedData, ivSize, cipherText, 0, cipherText.Length);
        
        var key = DeriveKeyFromPassphrase(passphrase, keySize);
        
        aes.Key = key;
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    private byte[] DeriveKeyFromPassphrase(string passphrase, int keySize)
    {
        const int iterations = 10000;
        var salt = Encoding.UTF8.GetBytes("GameMacroAssistantSalt2024"); // In production, use random salt
        
        using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keySize);
    }
    
    private string HashPassphrase(string passphrase)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
        return Convert.ToBase64String(hashBytes);
    }
    
    private string GenerateReadableFormat(Macro macro)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Macro: {macro.Name}");
        sb.AppendLine($"Description: {macro.Description}");
        sb.AppendLine($"Created: {macro.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Steps: {macro.Steps.Count}");
        sb.AppendLine();
        
        foreach (var step in macro.Steps.OrderBy(s => s.Order))
        {
            sb.AppendLine($"{step.Order + 1}. {GetStepDescription(step)}");
        }
        
        return sb.ToString();
    }
    
    private string GetStepDescription(Step step)
    {
        return step switch
        {
            MouseStep mouse => $"Mouse {mouse.Action} at ({mouse.AbsolutePosition.X}, {mouse.AbsolutePosition.Y})",
            KeyboardStep keyboard => $"Key {keyboard.VirtualKeyCode} {keyboard.Action}",
            DelayStep delay => $"Wait {delay.DelayMs}ms",
            ConditionalStep conditional => $"Image match (threshold: {conditional.MatchThreshold:P0})",
            _ => "Unknown step"
        };
    }
}