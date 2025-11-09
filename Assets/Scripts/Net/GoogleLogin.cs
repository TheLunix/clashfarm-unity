using UnityEngine;
using System.Collections;

#if USE_GOOGLE_SIGNIN
using Google; // з плагіна
#endif

public class GoogleLogin : MonoBehaviour
{
    [SerializeField] private string webClientId =
        "717687880808-b6e5qqsarg06pq91oss8agikgo4bj8vp.apps.googleusercontent.com";

    public void SignIn()
    {
#if USE_GOOGLE_SIGNIN && UNITY_ANDROID && !UNITY_EDITOR
        var cfg = new GoogleSignInConfiguration {
            WebClientId    = webClientId,
            RequestIdToken = true,
            RequestEmail   = true
        };
        GoogleSignIn.Configuration = cfg;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsFaulted) { Debug.LogError(task.Exception); return; }
            var account = task.Result;
            var idToken = account.IdToken;

            // тут або вхід, або прив’язка. Зараз — прив’язка:
            StartCoroutine(ApiClient.Instance.LinkGoogleAccount(idToken));
            // якщо захочеш вхід: StartCoroutine(ApiClient.Instance.LoginWithGoogle(idToken));
        });
#else
        Debug.LogWarning("Google Sign-In запускай на Android пристрої (UNITY_ANDROID). У редакторі він недоступний.");
#endif
    }
}
