using System.Collections.Concurrent;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Services;

public class FirebaseKidsPushOptions
{
    public const string SectionName = "Firebase";
    /// <summary>
    /// Conteúdo JSON da Service Account do Firebase (preferido em containers/Coolify).
    /// Se definido, CredentialsPath é ignorado.
    /// </summary>
    public string? CredentialsJson { get; set; }
    /// <summary>
    /// Caminho para o arquivo JSON da Service Account. Usado quando CredentialsJson está vazio.
    /// </summary>
    public string? CredentialsPath { get; set; }
}

public class KidsPushNotificationService : IKidsPushNotificationService
{
    private static bool _firebaseInitialized;
    private static readonly object _initLock = new();
    private readonly FirebaseKidsPushOptions _options;
    private readonly IKidsDeviceTokenRepository _tokenRepository;
    private readonly ILogger<KidsPushNotificationService> _logger;

    public KidsPushNotificationService(
        IOptions<FirebaseKidsPushOptions> options,
        IKidsDeviceTokenRepository tokenRepository,
        ILogger<KidsPushNotificationService> logger)
    {
        _options = options?.Value ?? new FirebaseKidsPushOptions();
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    public async Task SendToPessoasAsync(
        IEnumerable<int> pessoaIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null)
    {
        var tokens = (await _tokenRepository.GetTokensByPessoaIdsAsync(pessoaIds)).ToList();
        if (tokens.Count == 0) return;

        if (!EnsureFirebaseApp())
        {
            _logger.LogWarning("Firebase não configurado (CredentialsPath vazio). Push não enviado.");
            return;
        }

        var messaging = FirebaseMessaging.DefaultInstance;
        var dataDict = data != null ? new Dictionary<string, string>(data) : new Dictionary<string, string>();

        foreach (var token in tokens)
        {
            try
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = dataDict.Count > 0 ? dataDict : null
                };
                var id = await messaging.SendAsync(message);
                _logger.LogInformation("Push enviado. MessageId: {MessageId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar push para um dispositivo. Token pode estar inválido.");
            }
        }
    }

    private bool EnsureFirebaseApp()
    {
        if (_firebaseInitialized) return true;

        lock (_initLock)
        {
            if (_firebaseInitialized) return true;
            try
            {
                GoogleCredential? credential = null;

                var json = _options?.CredentialsJson?.Trim();
                if (!string.IsNullOrEmpty(json))
                {
                    credential = GoogleCredential.FromJson(json);
                }
                else
                {
                    var path = _options?.CredentialsPath?.Trim();
                    if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        return false;
                    credential = GoogleCredential.FromFile(path);
                }

                if (FirebaseApp.DefaultInstance == null)
                    FirebaseApp.Create(new AppOptions { Credential = credential });
                _firebaseInitialized = true;
                _logger.LogInformation("Firebase App inicializado para push.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar Firebase App.");
                return false;
            }
        }
    }
}
