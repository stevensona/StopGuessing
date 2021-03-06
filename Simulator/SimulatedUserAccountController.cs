﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StopGuessing.Controllers;
using StopGuessing.DataStructures;
using StopGuessing.EncryptionPrimitives;
using StopGuessing.AccountStorage.Memory;
using StopGuessing.Interfaces;
using StopGuessing.Models;
using StopGuessing.Utilities;

namespace Simulator
{
    public class SimulatedUserAccountControllerFactory : IFactory<SimulatedUserAccountController>
    {

        public SimulatedUserAccountController Create()
        {
            return new SimulatedUserAccountController();
        }
    }

    public class SimulatedUserAccountController : IUserAccountController<SimulatedUserAccount> //<SimulatedUserAccount>
    {
        public SimulatedUserAccountController()
        {
        }

        public SimulatedUserAccount Create(
            string usernameOrAccountId,
            string password = null,
            int? maxNumberOfCookiesToTrack = null,
            int? maxFailedPhase2HashesToTrack = null,
            DateTime? currentDateTimeUtc = null)
        {
            SimulatedUserAccount account = new SimulatedUserAccount
            {
                UsernameOrAccountId = usernameOrAccountId,
                Password = password,
                HashesOfCookiesOfClientsThatHaveSuccessfullyLoggedIntoThisAccount =
                    new ConcurrentDictionary<string,bool>(),
                RecentIncorrectPhase2Hashes =
                    new SmallCapacityConstrainedSet<string>(maxFailedPhase2HashesToTrack ??
                                                            UserAccountController<SimulatedUserAccount>
                                                                .DefaultMaxFailedPhase2HashesToTrack),
                ConsumedCredits = new DecayingDouble(0, currentDateTimeUtc),
                NumberOfIterationsToUseForPhase1Hash = 1
            };



            return account;
        }


        public byte[] ComputePhase1Hash(SimulatedUserAccount userAccount, string password)
        {
            return Encoding.UTF8.GetBytes(password);
        }

        public string ComputePhase2HashFromPhase1Hash(SimulatedUserAccount account, byte[] phase1Hash)
        {
            return Encoding.UTF8.GetString(phase1Hash);
        }

        /// <summary>
        /// Sets the password of a user.
        /// <b>Important</b>: this does not authenticate the user but assumes the user has already been authenticated.
        /// The <paramref name="oldPassword"/> field is used only to optionally recover the EC symmetricKey, not to authenticate the user.
        /// </summary>
        /// <param name="userAccount"></param>
        /// <param name="newPassword">The new password to set.</param>
        /// <param name="oldPassword">If this optional field is provided and correct, the old password will allow us to re-use the old log decryption symmetricKey.
        /// <b>Providing this parameter will not cause this function to authenticate the user first.  The caller must do so beforehand.</b></param>
        /// <param name="nameOfExpensiveHashFunctionToUse">The name of the phase 1 (expenseive) hash to use.</param>
        /// <param name="numberOfIterationsToUseForPhase1Hash">The number of iterations that the hash should be performed.</param>
        public void SetPassword(
            SimulatedUserAccount userAccount,
            string newPassword,
            string oldPassword = null,
            string nameOfExpensiveHashFunctionToUse = null,
            int? numberOfIterationsToUseForPhase1Hash = null)
        {
            byte[] newPasswordHashPhase1 = ComputePhase1Hash(userAccount, newPassword);

            // Calculate the Phase2 hash by hasing the phase 1 hash with SHA256.
            // We can store this without revealing the phase 1 hash used to encrypt the EC account log symmetricKey.
            // We can use it to verify whether a provided password is correct
            userAccount.PasswordHashPhase2 = ComputePhase2HashFromPhase1Hash(userAccount, newPasswordHashPhase1);            
        }


        /// <summary>
        /// Set the EC account log key
        /// </summary>
        /// <param name="userAccount"></param>
        /// <param name="accountLogKey"></param>
        /// <param name="phase1HashOfCorrectPassword">The phase 1 hash of the correct password</param>
        /// <returns></returns>
        public virtual void SetAccountLogKey(
            SimulatedUserAccount userAccount,
            Encryption.IPrivateKey accountLogKey,
            byte[] phase1HashOfCorrectPassword)
        {            
        }

        /// <summary>
        /// Derive the EC private account log key from the phase 1 hash of the correct password.
        /// </summary>
        /// <param name="userAccount"></param>
        /// <param name="phase1HashOfCorrectPassword">The phase 1 hash of the correct password</param>
        /// <returns></returns>
        public Encryption.IPrivateKey DecryptPrivateAccountLogKey(
            SimulatedUserAccount userAccount,
            byte[] phase1HashOfCorrectPassword)
        {
            return null;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> AddIncorrectPhaseTwoHashAsync(SimulatedUserAccount userAccount, string phase2Hash,
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            DateTime? whenSeenUtc = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return userAccount.RecentIncorrectPhase2Hashes.Add(phase2Hash);
            //return await TaskHelper.PretendToBeAsync(userAccount.RecentIncorrectPhase2Hashes.Add(phase2Hash));
        }

        public bool AddIncorrectPhaseTwoHash(SimulatedUserAccount userAccount, string phase2Hash,
            DateTime? whenSeenUtc = null)
        {
            return userAccount.RecentIncorrectPhase2Hashes.Add(phase2Hash);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> HasClientWithThisHashedCookieSuccessfullyLoggedInBeforeAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            SimulatedUserAccount userAccount,
            string hashOfCookie,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return userAccount.HashesOfCookiesOfClientsThatHaveSuccessfullyLoggedIntoThisAccount.ContainsKey(hashOfCookie);
        }

        public bool HasClientWithThisHashedCookieSuccessfullyLoggedInBefore(
            SimulatedUserAccount userAccount,
            string hashOfCookie)
        {
            return userAccount.HashesOfCookiesOfClientsThatHaveSuccessfullyLoggedIntoThisAccount.ContainsKey(hashOfCookie);
        }

#pragma warning disable 1998
        public async Task RecordHashOfDeviceCookieUsedDuringSuccessfulLoginAsync(
            SimulatedUserAccount account, 
            string hashOfCookie,
#pragma warning restore 1998
            DateTime? whenSeenUtc = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            account.HashesOfCookiesOfClientsThatHaveSuccessfullyLoggedIntoThisAccount[hashOfCookie] = true;
        }

        public virtual void RecordHashOfDeviceCookieUsedDuringSuccessfulLoginBackground(
    SimulatedUserAccount userAccount,
    string hashOfCookie,
    DateTime? whenSeenUtc = null)
        {
            userAccount.HashesOfCookiesOfClientsThatHaveSuccessfullyLoggedIntoThisAccount[hashOfCookie] = true;
            //TaskHelper.RunInBackground(
            //    RecordHashOfDeviceCookieUsedDuringSuccessfulLoginAsync(userAccount, hashOfCookie, whenSeenUtc));
        }


#pragma warning disable 1998
        public async Task<double> TryGetCreditAsync(SimulatedUserAccount userAccount,
            double amountRequested,
            DateTime? timeOfRequestUtc = null,
            CancellationToken cancellationToken = default(CancellationToken))
#pragma warning restore 1998
        {
            DateTime timeOfRequestOrNowUtc = timeOfRequestUtc ?? DateTime.UtcNow;
            double amountAvailable = Math.Max(0, userAccount.CreditLimit - userAccount.ConsumedCredits.GetValue(userAccount.CreditHalfLife, timeOfRequestOrNowUtc));
            double amountConsumed = Math.Min(amountRequested, amountAvailable);
            userAccount.ConsumedCredits.SubtractInPlace(userAccount.CreditHalfLife, amountConsumed, timeOfRequestOrNowUtc);
            return amountConsumed;
        }
    }
}
