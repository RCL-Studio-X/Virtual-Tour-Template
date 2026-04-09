using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.Events;

namespace StudioXRCL.VirtualTour.Utilities
{
    public class HMDRemovalDetector : MonoBehaviour
    {
        /// <summary>
        /// Event invoked when the headset is removed.
        /// </summary>
        public UnityEvent OnRemoval;
        /// <summary>
        /// Event invoked when the headset is put back on after being removed.
        /// </summary>
        public UnityEvent OnWake;
        /// <summary>
        /// The InputDevice representing the HMD.
        /// </summary>
        private InputDevice _hmd;
        /// <summary>
        /// Tracks whether the user was present in the previous frame to detect changes in presence.
        /// </summary>
        private bool _wasUserPresent = true;

        /// <summary>
        /// Calls <see cref="InitializeHMD"/>.
        /// </summary>
        private void Start()
        {
            InitializeHMD();
        }

        /// <summary>
        /// Initializes the _hmd variable. If no HMD is found, it logs a warning and disables the script.
        /// </summary>
        private void InitializeHMD()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count > 0)
                _hmd = devices[0];
            else {
                Debug.LogWarning("No HMD found. HMD removal detection will not work.");
                enabled = false;
            }
        }

        /// <summary>
        /// Checks the user's presence status each frame.
        /// </summary>
        private void Update()
        {
            CheckUserPresence();
        }

        /// <summary>
        /// Checks the user's presence status using the HMD's userPresence feature.
        /// </summary>
        private void CheckUserPresence()
        {

            if (!_hmd.isValid)
            {
                InitializeHMD();
                return;
            }

            if (_hmd.TryGetFeatureValue(CommonUsages.userPresence, out bool isPresent))
            {
                if (_wasUserPresent && !isPresent)
                {
                    Debug.Log("Headset removed");
                    OnRemoval?.Invoke();
                }

                if (!_wasUserPresent && isPresent)
                {
                    Debug.Log("Headset put back on");
                    OnWake?.Invoke();
                }

                _wasUserPresent = isPresent;
            }
        }
    }
}
