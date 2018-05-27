using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A dual function class.
/// Manages the current players in the game,
/// and contains the factory methods to instantiate and restore
/// player objects.
/// </summary>
public class PlayerManager : MonoBehaviour, ICollection<Player>
{
    #region Unity Bindings

    [SerializeField] GameObject m_playerUi;
    [SerializeField] GameObject m_playerUiParent;

    // player prefabs
    [SerializeField] GameObject m_humanPlayer;
    [SerializeField] GameObject m_aiPlayer;

    #endregion

    #region Private Fields

    Player[] _players;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the player using the given ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    public Player this[int id] => _players[id];

    /// <summary>
    /// Checks if there is a winner in the game.
    /// A winner is found when there is only one player who is not eliminated.
    /// </summary>
    public Player Winner
    {
        get
        {
            var activePlayers = this.Where(p => !p.IsEliminated);
            return activePlayers.Count() > 1 ? null : activePlayers.First();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the players with the given setup.
    /// </summary>
    /// <param name="playerSetup">The player setup data.</param>
    public void InitPlayers(IEnumerable<Tuple<PlayerKind, Color>> playerSetup)
    {
        if (_players != null)
            throw new InvalidOperationException("Cannot re-initialize players");
        List<Player> newPlayers = new List<Player>();
        int currentId = 0;
        foreach (var setup in playerSetup)
            newPlayers.Add(InitPlayer(setup.Item1, currentId++, setup.Item2)); // init player with current id, increment is done after return
        _players = newPlayers.ToArray();
    }

    /// <summary>
    /// Initializes the player.
    /// </summary>
    /// <returns>The initialized player.</returns>
    /// <param name="kind">The kind of player to initialize.</param>
    /// <param name="id">The ID of the player.</param>
    /// <param name="color">The colour of the player.</param>
    Player InitPlayer(PlayerKind kind, int id, Color color)
    {
        GameObject go;
        switch (kind)
        {
            case PlayerKind.Human:
                go = m_humanPlayer;
                break;
            case PlayerKind.AI:
                go = m_aiPlayer;
                break;
            default:
                throw new ArgumentException("Kind given is not valid");
        }
        Player currentPlayer = Instantiate(go, transform).GetComponent<Player>();
        PlayerUI playerUI = Instantiate(m_playerUi, m_playerUiParent.transform).GetComponent<PlayerUI>();
        currentPlayer.Init(id, color, playerUI);
        playerUI.Init(id, currentPlayer);
        return currentPlayer;
    }

    #endregion

    #region Serialization

    public SerializablePlayerManager CreateMemento()
    {
        return new SerializablePlayerManager
        {
            players = _players.Select(p => p.CreateMemento()).ToArray()
        };
    }

    public void RestoreMemento(SerializablePlayerManager memento)
    {
        _players = new Player[memento.players.Length];
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = InitPlayer(memento.players[i].kind, memento.players[i].id, memento.players[i].color);
            _players[i].RestoreMemento(memento.players[i]);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Takes in the current player ID and changes the value to the
    /// next player. WARNING: This alters the value in-place.
    /// </summary>
    /// <param name="currentPlayerId">Current player ID.</param>
    public void ToNextPlayer(ref int currentPlayerId)
    {
        // disable current player's ui
        this[currentPlayerId].Gui.IsActive = false;
        // for loop starts 1 player next, and stops back on the current player
        // this is to allow for turn skips to happen with 2 players in the game
        for (int i = 1; i < Count + 1; i++)
        {
            int nextPlayerId = (currentPlayerId + i) % Count;
            if (!this[nextPlayerId].IsEliminated)
            {
                if (this[nextPlayerId].SkipNextTurn)
                    this[nextPlayerId].SkipNextTurn = false;
                else
                {
                    currentPlayerId = nextPlayerId;
                    this[currentPlayerId].Gui.IsActive = true;
                    return; // we got the next player
                }
            }
        }
        throw new InvalidOperationException(); // unable to move to next player
    }

    /// <summary>
    /// Updates all the player GUIs.
    /// </summary>
    public void UpdateGUIs()
    {
        // update all players' GUIs
        foreach (Player player in this)
            player.Gui.UpdateDisplay();
    }

    #endregion

    #region Collection Methods

    public int Count => _players.Length;

    public bool IsReadOnly => _players.IsReadOnly;

    public bool Contains(Player item)
    {
        return ((ICollection<Player>)_players).Contains(item);
    }

    public void CopyTo(Player[] array, int arrayIndex)
    {
        ((ICollection<Player>)_players).CopyTo(array, arrayIndex);
    }

    public IEnumerator GetEnumerator()
    {
        return _players.GetEnumerator();
    }

    IEnumerator<Player> IEnumerable<Player>.GetEnumerator()
    {
        return ((ICollection<Player>)_players).GetEnumerator();
    }

    // bad operations required for ICollection

    public void Add(Player item)
    {
        throw new InvalidOperationException();
    }

    public void Clear()
    {
        throw new InvalidOperationException();
    }

    public bool Remove(Player item)
    {
        throw new InvalidOperationException();
    }

    #endregion
}
