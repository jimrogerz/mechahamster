﻿// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Unity.Editor;


namespace Hamster.States {
  // Utility state, for fetching structures from the database.
  // Returns the result in the result struct.
  class WaitingForDBLoad<T> : BaseState {

    protected bool isComplete = false;
    protected bool wasSuccessful = false;
    protected T result = default(T);
    protected string path;
    protected int failedFetches = 0;

    const int MaxDatabaseRetries = 5;

    protected Firebase.Database.FirebaseDatabase database;

    Menus.SingleLabelGUI menuComponent;

    public WaitingForDBLoad(string path) {
      this.path = path;
    }

    // Initialization method.  Called after the state
    // is added to the stack.
    public override void Initialize() {
      database = Firebase.Database.FirebaseDatabase.GetInstance(CommonData.app);
      database.GetReference(path).ValueChanged += HandleResult;

      menuComponent = SpawnUI<Menus.SingleLabelGUI>(StringConstants.PrefabsSingleLabelMenu);
      UpdateLabelText();
    }

    protected virtual void HandleResult(object sender,
      Firebase.Database.ValueChangedEventArgs args) {
      // Remove the listener as soon as we get a response.
      database.GetReference(path).ValueChanged -= HandleResult;

      if (args.DatabaseError != null) {
        Debug.LogError("Database error :" + args.DatabaseError.Code + ":\n" +
          args.DatabaseError.Message + "\n" + args.DatabaseError.Details);
      } else {
        wasSuccessful = true;
        if (args.Snapshot != null) {
          string json = args.Snapshot.GetRawJsonValue();
          if (!string.IsNullOrEmpty(json)) {
            result = JsonUtility.FromJson<T>(json);
          }
        }
      }
      isComplete = true;
    }

    // Called once per frame when the state is active.
    public override void Update() {
      if (isComplete) {
        manager.PopState();
      } else {
        UpdateLabelText();
      }
    }

    private void UpdateLabelText() {
      if (menuComponent != null) {
        menuComponent.LabelText.text =
          StringConstants.LabelLoading + Utilities.StringHelper.CycleDots();
      }
    }

    public override StateExitValue Cleanup() {
      DestroyUI();
      return new StateExitValue(
        typeof(WaitingForDBLoad<T>), new Results(path, result, wasSuccessful));
    }

    // Class for encapsulating the results of the database load, as
    // well as information about whether the load was successful
    // or not.
    public class Results {
      public string path;
      public T results;
      public bool wasSuccessful;

      public Results(string path, T results, bool wasSuccessful) {
        this.path = path;
        this.results = results;
        this.wasSuccessful = wasSuccessful;
      }
    }

  }
}
