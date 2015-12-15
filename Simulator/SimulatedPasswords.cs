﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StopGuessing.Controllers;
using StopGuessing.DataStructures;

namespace Simulator
{
    /// <summary>
    /// This class tracks information about passwords to be used by the simulator.
    /// Specifically, it reads in a password distribution for simulating user's password choices.
    /// </summary>
    public class SimulatedPasswords
    {
        private WeightedSelector<string> _passwordSelector;
        private List<string> _passwordsAlreadyKnownToBePopular;
        public List<string> OrderedListOfMostCommonPasswords;
        private WeightedSelector<string> _commonPasswordSelector;
        private DebugLogger _logger;

        public SimulatedPasswords(DebugLogger logger, ExperimentalConfiguration experimentalConfiguration)
        {
            _logger = logger;
            _logger.WriteStatus("Loading popular password file");
            LoadPasswordSelector(experimentalConfiguration.PasswordFrequencyFile);
            _logger.WriteStatus("Loading passwords known to be common by the algorithm before the attack");
            LoadKnownPopularPasswords(experimentalConfiguration.PreviouslyKnownPopularPasswordFile);
            _logger.WriteStatus("Creating common password selector");
            _commonPasswordSelector = _passwordSelector.TrimToInitialItems(
                    (int)experimentalConfiguration.NumberOfPopularPasswordsForAttackerToExploit);
            _logger.WriteStatus("Finished creating common password selector");

            _logger.WriteStatus("Creating list of most common passwords");
            OrderedListOfMostCommonPasswords =
                _passwordSelector.GetItems((int)experimentalConfiguration.NumberOfPopularPasswordsForAttackerToExploit);
            _logger.WriteStatus("Finished creating list of most common passwords");
        }


        /// <summary>
        /// Gets a password from a realistic password distribution.
        /// </summary>
        /// <returns>A password string</returns>
        public string GetPasswordFromWeightedDistribution()
        {
            return _passwordSelector.GetItemByWeightedRandom();
        }

        /// <summary>
        /// This method loads in the file containing passwords that StopGuessing knew were popular
        /// before the simulation begins
        /// </summary>
        /// <param name="pathToPreviouslyKnownPopularPasswordFile"></param>
        public void LoadKnownPopularPasswords(string pathToPreviouslyKnownPopularPasswordFile)
        {
            _passwordsAlreadyKnownToBePopular = new List<string>();
            using (System.IO.StreamReader file =
                new System.IO.StreamReader(pathToPreviouslyKnownPopularPasswordFile))
            {

                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
                        _passwordsAlreadyKnownToBePopular.Add(line);
                }
            }
        }

        /// <summary>
        /// This method will prime the simulator with known-popular passwords so that they are treated
        /// as if the simulator had already observed them (or been configured with them)
        /// </summary>
        /// <param name="loginAttemptController"></param>
        /// <returns></returns>
        public async Task PrimeWithKnownPasswordsAsync(LoginAttemptController loginAttemptController)
        {
            await TaskParalllel.ForEachWithWorkers(_passwordsAlreadyKnownToBePopular, async (password, itemNumer, cancelToken) =>
                await loginAttemptController.PrimeCommonPasswordAsync(password, 100, cancelToken));
        }

        /// <summary>
        /// Given the path of a file containing a count, followed by a space, followed by a password,
        /// this method reads in the distribution and creates a password selector that can provide
        /// passwords sampled from that distribution.
        /// </summary>
        /// <param name="pathToWeightedFrequencyFile"></param>
        private void LoadPasswordSelector(string pathToWeightedFrequencyFile)
        {
            _passwordSelector = new WeightedSelector<string>();
            // Created a weighted-random selector for paasswords based on the RockYou database.
            using (System.IO.StreamReader file =
                new System.IO.StreamReader(pathToWeightedFrequencyFile))
            {
                string lineWithCountFollowedBySpaceFollowedByPassword;
                while ((lineWithCountFollowedBySpaceFollowedByPassword = file.ReadLine()) != null)
                {
                    lineWithCountFollowedBySpaceFollowedByPassword =
                        lineWithCountFollowedBySpaceFollowedByPassword.Trim();
                    int indexOfFirstSpace = lineWithCountFollowedBySpaceFollowedByPassword.IndexOf(' ');
                    if (indexOfFirstSpace < 0 ||
                        indexOfFirstSpace + 1 >= lineWithCountFollowedBySpaceFollowedByPassword.Length)
                        continue; // The line is invalid as it doesn't have a space with a password after it
                    string countAsString = lineWithCountFollowedBySpaceFollowedByPassword.Substring(0, indexOfFirstSpace);
                    ulong count;
                    if (!ulong.TryParse(countAsString, out count))
                        continue; // The count field is invalid as it doesn't parse to an unsigned number
                    string password = lineWithCountFollowedBySpaceFollowedByPassword.Substring(indexOfFirstSpace + 1);
                    _passwordSelector.AddItem(password, count);
                }
            }
        }



    }
}