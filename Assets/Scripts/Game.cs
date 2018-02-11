﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {

    public Player[] players;
	public GameObject gameMap;
    public Player currentPlayer;

    public enum TurnState { Move1, Move2, EndOfTurn, NULL };
    [SerializeField] private TurnState turnState;
    [SerializeField] private bool gameFinished = false;
    [SerializeField] private bool testMode = false;

    public GameObject GUIHandler;

    public bool triggerDialog = false;

    Dialog dialog;


    public bool[] eliminatedPlayers;

    public TurnState GetTurnState() {
        return turnState;
    }

    public void SetTurnState(TurnState turnState) {
        this.turnState = turnState;
    }

    public bool IsFinished() {
        return gameFinished;
    }

    public void EnableTestMode() {
        testMode = true;
    }

    public void DisableTestMode() {
        testMode = false;
    }



    public void CreatePlayers(int numberOfPlayers){

        // ensure that the specified number of players
        // is at least 2 and does not exceed 4
        if (numberOfPlayers < 2)
            numberOfPlayers = 2;

        if (numberOfPlayers > 4) 
            numberOfPlayers = 4;

        // mark the specified number of players as human
        for (int i = 0; i < numberOfPlayers; i++)
        {
            players[i].SetHuman(true);
        }

        // give all players a reference to this game
		// and initialize their GUIs
		for (int i = 0; i < 4; i++)
		{
			players[i].SetGame(this);
			players[i].GetGui().Initialize(players[i], i + 1);
		}

    }

	public void InitializeMap() {

        // initialize all sectors, allocate players to landmarks,
        // and spawn units


		// get an array of all sectors
        Sector[] sectors = gameMap.GetComponentsInChildren<Sector>();

		// initialize each sector
        foreach (Sector sector in sectors)
		{
            sector.Initialize();
		}
            
		// get an array of all sectors containing landmarks
        Sector[] landmarkedSectors = GetLandmarkedSectors(sectors);
            
        // ensure there are at least as many landmarks as players
        if (landmarkedSectors.Length < players.Length)
        {
            throw new System.Exception("Must have at least as many landmarks as players; only " + landmarkedSectors.Length.ToString() + " landmarks found for " + players.Length.ToString() + " players.");
        }


		// randomly allocate sectors to players
        foreach (Player player in players) 
		{
			bool playerAllocated = false;
            while (!playerAllocated) {
                
				// choose a landmarked sector at random
                int randomIndex = Random.Range (0, landmarkedSectors.Length);
				
                // if the sector is not yet allocated, allocate the player
                if (((Sector) landmarkedSectors[randomIndex]).GetOwner() == null)
				{
                    player.Capture(landmarkedSectors[randomIndex]);
					playerAllocated = true;
				}

                // retry until player is allocated
			}
		}

		// spawn units for each player
        foreach (Player player in players)
        {
            player.SpawnUnits();
        }

        eliminatedPlayers = new bool[players.Length]; // Automatically set to false for all

        // Create an instance of the Dialog
        dialog = GUIHandler.GetComponent<Dialog>();

	}

    private Sector[] GetLandmarkedSectors(Sector[] sectors) {

        // return a list of all sectors that contain landmarks from the given array

        List<Sector> landmarkedSectors = new List<Sector>();
        foreach (Sector sector in sectors)
        {
            if (sector.GetLandmark() != null)
            {
                landmarkedSectors.Add(sector);
            }
        }

        return landmarkedSectors.ToArray();
    }

    public bool NoUnitSelected() {
        
        // return true if no unit is selected, false otherwise


        // scan through each player
        foreach (Player player in players)
        {
            // scan through each unit of each player
            foreach (Unit unit in player.units)
            {
                // if a selected unit is found, return false
                if (unit.IsSelected() == true)
                    return false;
            }
        }

        // otherwise, return true
        return true;
    }

    public void NextPlayer() {

        // set the current player to the next player in the order


        // deactivate the current player
        currentPlayer.SetActive(false);
		currentPlayer.GetGui().Deactivate();

        // find the index of the current player
       
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == currentPlayer)
            {
                // set the next player's index
                int nextPlayerIndex = (i + 1) % players.Length;

                // Code replaced by Jack, unnecessary if statements 
                currentPlayer = players[nextPlayerIndex];
                currentPlayer.SetActive(true);
                currentPlayer.GetGui().Activate();
                break;

            }
        }
    }
       
    public void NextTurnState() {

        // change the turn state to the next in the order,
        // or to initial turn state if turn is completed

        switch (turnState)
        {
            case TurnState.Move1:
                turnState = TurnState.Move2;
                break;

            case TurnState.Move2:
                turnState = TurnState.EndOfTurn;
                break;

            case TurnState.EndOfTurn:
                turnState = TurnState.Move1;
                break;

            default:
                break;
        }

        #region Remove defeated players and check if the game was won (Added by Jack 01/02/2018)

        CheckForDefeatedPlayers();

        Player winner = GetWinner();
        if (winner != null)
        {
            EndGame();
        }

        #endregion

        UpdateGUI();
    }

    public void EndTurn() {

        // end the current turn

        turnState = TurnState.EndOfTurn;
    }

    #region Function to check for defeated players and notify the others (Added by Jack 01/02/2018)

    public void CheckForDefeatedPlayers()
    {
    // Checks if any players were defeated that turn state and removes them whilst notifying the rest of the players
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsEliminated() && eliminatedPlayers[i] == false)
            {
                // Set up the dialog box and show it
                dialog.setDialogType(Dialog.DialogType.PlayerElimated);
                dialog.setPlayerName(players[i].name);
                dialog.Show();
                eliminatedPlayers[i] = true; // Used to ensure that the dialog is only shown once
                players[i].Defeat(currentPlayer); // Releases all owned sectors
            }
        }
    }

    #endregion

    public Player GetWinner() {

        // return the winning player, or null if no winner yet

        Player winner = null;

        // scan through each player
        foreach (Player player in players)
        {
            // if the player hasn't been eliminated
            if (!player.IsEliminated())
            {
                // if this is the first player found that hasn't been eliminated,
                // assume the player is the winner
                if (winner == null)
                    winner = player;

                // if another player that was not eliminated was already,
                // found, then return null
                else
                    return null;
            }
        }

        // if only one player hasn't been eliminated, then return it as the winner
        return winner;
    }

    public void EndGame() {
        gameFinished = true;

        #region Show the winner dialog (Added by Jack 01/02/2018)

        dialog.setDialogType(Dialog.DialogType.EndGame);
        dialog.setPlayerName(GetWinner().name);
        dialog.Show();

        #endregion

        currentPlayer.SetActive(false);
        currentPlayer = null;
        turnState = TurnState.NULL;
        Debug.Log("GAME FINISHED");
    }

	public void UpdateGUI() {

		// update all players' GUIs
		for (int i = 0; i < 4; i++) {
			players [i].GetGui ().UpdateDisplay ();
		}
	}
        
    public void Initialize () {
        
        // initialize the game


        // create a specified number of human players
        // *** currently hard-wired to 2 for testing ***
        CreatePlayers(2);

        // initialize the map and allocate players to landmarks
        InitializeMap();

        // initialize the turn state
        turnState = TurnState.Move1;

        // set Player 1 as the current player
        currentPlayer = players[0];
		currentPlayer.GetGui().Activate();
        players[0].SetActive(true);

		// update GUIs
		UpdateGUI();

	}

        
    void Update () {

        // at the end of each turn, check for a winner and end the game if
        // necessary; otherwise, start the next player's turn

        if (triggerDialog)
        {
            triggerDialog = false;
            dialog.setDialogType(Dialog.DialogType.EndGame);
            dialog.setPlayerName("PLAYER 1");
            dialog.Show();
        }
		
        // if the current turn has ended and test mode is not enabled
        if (turnState == TurnState.EndOfTurn && !testMode)
        {
            
            // if there is no winner yet
            if (GetWinner() == null)
            {
                // start the next player's turn
                NextTurnState();
                NextPlayer();

                // skip eliminated players
                while (currentPlayer.IsEliminated())
                    NextPlayer();

                // spawn units for the next player
                currentPlayer.SpawnUnits();
            }
            else
                if (!gameFinished)
                    EndGame();
        }
	}

    public void UpdateAccessible () {

        // copy of Update that can be called by other objects (for testing)

        if (turnState == TurnState.EndOfTurn)
        {
            // if there is no winner yet
            if (GetWinner() == null)
            {
                // start the next player's turn
                NextPlayer();
                NextTurnState();

                // skip eliminated players
                while (currentPlayer.IsEliminated())
                    NextPlayer();

                // spawn units for the next player
                currentPlayer.SpawnUnits();
            }
            else
                if (!gameFinished)
                    EndGame();
        }
    }

}
