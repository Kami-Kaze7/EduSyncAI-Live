using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace EduSyncAI
{
    public class BiometricAuthenticationService
    {
        /// <summary>
        /// Checks if Windows Hello biometric authentication is available on this device
        /// </summary>
        public async Task<bool> IsBiometricAvailableAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability == UserConsentVerifierAvailability.Available;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Authenticates using Windows Hello (fingerprint, face, PIN)
        /// Returns true if authentication successful
        /// </summary>
        public async Task<bool> AuthenticateWithBiometricAsync(string message = "Please verify your identity")
        {
            try
            {
                var result = await UserConsentVerifier.RequestVerificationAsync(message);
                return result == UserConsentVerificationResult.Verified;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a user-friendly message about biometric availability
        /// </summary>
        public async Task<string> GetAvailabilityMessageAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability switch
                {
                    UserConsentVerifierAvailability.Available => "Windows Hello is available",
                    UserConsentVerifierAvailability.DeviceNotPresent => "No biometric device detected",
                    UserConsentVerifierAvailability.NotConfiguredForUser => "Windows Hello not set up for this user",
                    UserConsentVerifierAvailability.DisabledByPolicy => "Windows Hello disabled by policy",
                    UserConsentVerifierAvailability.DeviceBusy => "Biometric device is busy",
                    _ => "Windows Hello not available"
                };
            }
            catch
            {
                return "Unable to check Windows Hello availability";
            }
        }
    }
}
