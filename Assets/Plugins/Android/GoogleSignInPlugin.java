package com.yourcompany.googlesignin;

import android.app.Activity;
import androidx.annotation.NonNull;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import com.unity3d.player.UnityPlayer;

import java.util.concurrent.Executors;

/**
 * Android Credential Manager로 "Sign in with Google"을 수행하고, ID 토큰을 Unity로 돌려준다.
 * serverClientId = Web 클라이언트 ID 로 요청하므로 idToken의 aud가 Web client → Firebase가 수락.
 * 결과는 UnityPlayer.UnitySendMessage(gameObject, ...)로 Unity 메인스레드에 전달.
 *
 * NativeGoogleSignIn.cs 의 AndroidJavaClass("com.yourcompany.googlesignin.GoogleSignInPlugin")와
 * 패키지/클래스명이 일치해야 한다(바꾸려면 양쪽 동시에).
 */
public class GoogleSignInPlugin {

    public static void signIn(final String gameObject, final String webClientId) {
        final Activity activity = UnityPlayer.currentActivity;
        final CredentialManager credentialManager = CredentialManager.create(activity);

        GetSignInWithGoogleOption option =
                new GetSignInWithGoogleOption.Builder(webClientId).build();
        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(option)
                .build();

        credentialManager.getCredentialAsync(
                activity,
                request,
                null,
                Executors.newSingleThreadExecutor(),
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse response) {
                        try {
                            GoogleIdTokenCredential cred =
                                    GoogleIdTokenCredential.createFrom(response.getCredential().getData());
                            UnityPlayer.UnitySendMessage(gameObject, "OnGoogleSignInSuccess", cred.getIdToken());
                        } catch (Exception e) {
                            UnityPlayer.UnitySendMessage(gameObject, "OnGoogleSignInFailure", String.valueOf(e.getMessage()));
                        }
                    }

                    @Override
                    public void onError(@NonNull GetCredentialException e) {
                        UnityPlayer.UnitySendMessage(gameObject, "OnGoogleSignInFailure", String.valueOf(e.getMessage()));
                    }
                });
    }

    public static void signOut() {
        // 필요 시 CredentialManager.clearCredentialState(...) 로 자동선택 상태 초기화.
    }
}
