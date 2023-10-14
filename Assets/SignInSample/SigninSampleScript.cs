// <copyright file="SigninSampleScript.cs" company="Google Inc.">
// Copyright (C) 2017 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations

namespace SignInSample
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Facebook.Unity;
    using Google;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    public class SigninSampleScript : MonoBehaviour
    {
        public RawImage image;
        public Text displayName;
        public Text email;
        public Text uniqueId;

        public Text statusText;

        public string webClientId = "<your client id here>";

        private GoogleSignInConfiguration configuration;

        // Defer the configuration creation until Awake so the web Client ID
        // Can be set via the property inspector in the Editor.
        void Awake()
        {
            configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestProfile = true,
                RequestEmail = true,
                UseGameSignIn = false
            };

            FB.Init(OnFBInitComplete, OnFBHideUnity);
        }

        private void SetUserData(string _name, string _email, string _id, Uri _uri)
        {
            displayName.text = _name;
            email.text = _email;
            uniqueId.text = _id;
            StartCoroutine(SetImage(_uri));

            IEnumerator SetImage(Uri _uri)
            {
                if (_uri == null)
                {
                    image.texture = null;
                    yield break;
                }

                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_uri))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        AddStatusText("Profile Image Error: " + uwr.error);
                    }
                    else
                    {
                        // Get downloaded asset bundle
                        image.texture = DownloadHandlerTexture.GetContent(uwr);
                    }
                }
            }
        }


        #region Google
        public void OnSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            AddStatusText("Calling SignIn");

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
        }

        public void OnDisconnect()
        {
            AddStatusText("Calling Disconnect");
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using (IEnumerator<Exception> enumerator =
                        task.Exception.InnerExceptions.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                        AddStatusText("Got Error: " + error.Status + " " + error.Message);
                    }
                    else
                    {
                        AddStatusText("Got Unexpected Exception?!?" + task.Exception);
                    }
                }
            }
            else if (task.IsCanceled)
            {
                AddStatusText("Canceled");
            }
            else
            {
                AddStatusText("Welcome: " + task.Result);
                SetUserData(task.Result.DisplayName, task.Result.Email, task.Result.UserId, task.Result.ImageUrl);
            }
        }




        public void OnSignInSilently()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            AddStatusText("Calling SignIn Silently");

            GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(OnAuthenticationFinished);
        }


        public void OnGamesSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = true;
            GoogleSignIn.Configuration.RequestIdToken = false;

            AddStatusText("Calling Games SignIn");

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
        }


        public void OnSignOut()
        {
            AddStatusText("Calling Google SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
            SetUserData("", "", "", null);
        }


        private List<string> messages = new List<string>();
        void AddStatusText(string text)
        {
            Debug.Log(text);
            if (messages.Count == 5)
            {
                messages.RemoveAt(0);
            }
            messages.Add(text);
            string txt = "";
            foreach (string s in messages)
            {
                txt += "\n" + s;
            }
            statusText.text = txt;
        }
        #endregion



        #region Facebook
        private void OnFBHideUnity(bool isUnityShown)
        {
            AddStatusText("FB isUnityShown: " + isUnityShown);
        }

        private void OnFBInitComplete()
        {
            if (FB.IsLoggedIn) AddStatusText("FB Logged In");
            else AddStatusText("FB NOT Logged In");
        }

        public void OnSignInFacebook()
        {
            List<string> _permissions = new List<string> { "public_profile", "email" };
            FB.LogInWithReadPermissions(_permissions, FBLoginResult);
        }

        private void FBLoginResult(ILoginResult result)
        {
            if (result.Error != null)
            {
                AddStatusText("FB Login Error : " + result.Error);
                return;
            }

            if (FB.IsLoggedIn)
            {
                var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
                AddStatusText("FB Logged In: " + result.RawResult);
                //SetUserData(result.RawResult, result.AccessToken.UserId);
            }
            else AddStatusText("FB Login Failed : " + result.Error);
        }

        public void OnFBLogout()
        {
            if (FB.IsLoggedIn)
            {
                AddStatusText("Calling Facebook SignOut");
                FB.LogOut();
                SetUserData("Name:", "Email:", "Id:", null);
            }
            else AddStatusText("Facebook Not LoggedIn");
        }

        #endregion
    }
}
