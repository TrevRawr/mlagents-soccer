using System.Collections.Generic;
using UnityEngine;

public class SoccerAcademy : MLAgents.Academy
{
    // Store references to objects that need to be enabled/disabled in different curriculum lessons
    private ObstacleComponent[] obstacles;
    private SoccerPlayerAgent[] players;

    public override void InitializeAcademy()
    {
        MLAgents.Monitor.SetActive(true);
        obstacles = FindObjectsOfType<ObstacleComponent>();
        foreach (ObstacleComponent obstacle in obstacles) { obstacle.gameObject.SetActive(false); }

        players = FindObjectsOfType<SoccerPlayerAgent>();
        
        base.InitializeAcademy();
    }

    public void DisablePlayers()
    {
        foreach (SoccerPlayerAgent player in players)
        {
            if (player.CompareTag(Tags.AWAY_TEAM))
            {
                player.gameObject.SetActive(false);
            }
        }

        var stadiumMetaDatas = FindObjectsOfType<StadiumMetaData>();
        foreach (var stadiumMetaData in stadiumMetaDatas)
        {
            List<SoccerPlayerAgent> homePlayers = new List<SoccerPlayerAgent>(stadiumMetaData.GetComponentsInChildren<SoccerPlayerAgent>());
            homePlayers.RemoveAll((player) => !player.CompareTag(Tags.HOME_TEAM));

            List<SoccerPlayerAgent> awayPlayers = new List<SoccerPlayerAgent>(stadiumMetaData.GetComponentsInChildren<SoccerPlayerAgent>());
            awayPlayers.RemoveAll((player) => !player.CompareTag(Tags.AWAY_TEAM));

            // if not 3v3
            if (resetParameters[ResetParameters.THREE_V_THREE] < 1f)
            {
                // set every player inactive except one. Hopefully that one is the middle one... 
                for (int i = 1; i < homePlayers.Count; i++)
                {
                    homePlayers[i].gameObject.SetActive(false);
                }
                for (int i = 1; i < awayPlayers.Count; i++)
                {
                    awayPlayers[i].gameObject.SetActive(false);
                }
                //TODO: remove these inactive players from homePlayers and awayPlayers
                //TODO: while I'm at it, refactor this whole class mess
            }
        }
    }
    public override void AcademyReset()
    {
        base.AcademyReset();

        if (resetParameters.Count < 1)
        {
            return;
        }
        DisablePlayers();
        if (resetParameters[ResetParameters.ADD_OBSTACLES] >= 1.0f)
        {
            foreach (ObstacleComponent obstacle in obstacles)
            {
                obstacle.gameObject.SetActive(true);
            }

            var stadiumMetaDatas = FindObjectsOfType<StadiumMetaData>();
            foreach (var stadiumMetaData in stadiumMetaDatas)
            {
                List<SoccerPlayerAgent> soccerPlayers = new List<SoccerPlayerAgent>(stadiumMetaData.GetComponentsInChildren<SoccerPlayerAgent>());
                foreach (SoccerPlayerAgent agent in soccerPlayers)
                {   
                    ObstacleComponent[] stadiumObstacles = stadiumMetaData.GetComponentsInChildren<ObstacleComponent>();
                    if (stadiumObstacles.Length > agent.enemyGameObjects.Count)
                    {
                        Debug.LogWarning("Warning: More obstacles " + stadiumObstacles.Length + " than player enemy count " + agent.enemyGameObjects.Count);
                    }
                    for (int i = 1; i < agent.enemyGameObjects.Count; i++) // start at 1 to avoid overwriting obstacle set above 
                    {
                        if (i < stadiumObstacles.Length)
                        {
                            agent.enemyGameObjects[i] = this.obstacles[i].gameObject;
                        }
                        else
                        {
                            agent.enemyGameObjects[i] = null; // indicate that this observation value should be set to 1 i.e. enemies are as far away as possible
                        }
                    }
                }
            }
        }
        // prior to this, player enemies were set to dummy obstacles that the player was required to go around
        else if (resetParameters[ResetParameters.ADD_ENEMIES] >= 1.0f || resetParameters[ResetParameters.THREE_V_THREE] >= 1f)
        {
            // remove obstacles
            foreach (ObstacleComponent obstacle in obstacles)
            {
                obstacle.gameObject.SetActive(false);
            }

            // add all players
            foreach (SoccerPlayerAgent player in players)
            {
                player.gameObject.SetActive(true);
            }

            // set player team arrays so players know who they're playing against
            // important because this info is used in player perception
            var stadiumMetaDatas = FindObjectsOfType<StadiumMetaData>();
            foreach (var stadiumMetaData in stadiumMetaDatas)
            {
                List<SoccerPlayerAgent> homePlayers = new List<SoccerPlayerAgent>(stadiumMetaData.GetComponentsInChildren<SoccerPlayerAgent>());
                homePlayers.RemoveAll((player) => !player.CompareTag(Tags.HOME_TEAM));

                List<SoccerPlayerAgent> awayPlayers = new List<SoccerPlayerAgent>(stadiumMetaData.GetComponentsInChildren<SoccerPlayerAgent>());
                awayPlayers.RemoveAll((player) => !player.CompareTag(Tags.AWAY_TEAM));

                // if not 3v3
                if (resetParameters[ResetParameters.THREE_V_THREE] < 1f)
                {
                    // set every player inactive except one. Hopefully that one is the middle one... 
                    for (int i = 1; i < homePlayers.Count; i++)
                    {
                        homePlayers[i].gameObject.SetActive(false);
                    }
                    for (int i = 1; i < awayPlayers.Count; i++)
                    {
                        awayPlayers[i].gameObject.SetActive(false);
                    }
                    //TODO: remove these inactive players from homePlayers and awayPlayers
                    //TODO: while I'm at it, refactor this whole class mess
                }

                // set home player enemies to away team
                foreach (SoccerPlayerAgent homePlayer in homePlayers)
                {
                    for (int i = 0; i < awayPlayers.Count; i++)
                    {
                        if (i < awayPlayers.Count)
                        {
                            homePlayer.enemyGameObjects[i] = awayPlayers[i].gameObject;
                        }
                        else
                        {
                            homePlayer.enemyGameObjects[i] = null;
                        }
                    }
                }
                foreach (SoccerPlayerAgent homePlayer in homePlayers)
                {
                    int teamMateIndex = 0;
                    foreach (SoccerPlayerAgent teamMate in homePlayers)
                    {
                        if (homePlayer != teamMate)
                        {
                            homePlayer.teamMateGameObjects[teamMateIndex] = teamMate.gameObject;
                            teamMateIndex++;
                        }
                    }
                }

                // set away team enemies to home team
                foreach (SoccerPlayerAgent awayPlayer in awayPlayers)
                {
                    for (int i = 0; i < homePlayers.Count; i++)
                    {
                        if (i < homePlayers.Count)
                        {
                            awayPlayer.enemyGameObjects[i] = homePlayers[i].gameObject;
                        }
                        else
                        {
                            awayPlayer.enemyGameObjects[i] = awayPlayers[i].gameObject;
                        }
                    }
                }
                foreach (SoccerPlayerAgent awayPlayer in awayPlayers)
                {
                    int teamMateIndex = 0;
                    foreach (SoccerPlayerAgent teamMate in awayPlayers)
                    {
                        if (awayPlayer != teamMate)
                        {
                            awayPlayer.teamMateGameObjects[teamMateIndex] = teamMate.gameObject;
                            teamMateIndex++;
                        }
                    }
                }

            }
        }
        else
        {
            // if there are no obstacles, and no enemies, observations should be null
            foreach (SoccerPlayerAgent agent in players)
            {
                for (int i = 0; i < agent.enemyGameObjects.Count; i++)
                {
                    agent.enemyGameObjects[i] = null;
                }
            }
        }
    }
}
