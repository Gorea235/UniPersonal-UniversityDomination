﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(EffectManager))]
public class Sector : MonoBehaviour
{
    #region Unity Bindings

    [SerializeField] Sector[] m_adjacentSectors;
    [SerializeField] Landmark m_landmark;

    #endregion

    #region Private fields

    const float DefaultHighlightAmount = 0.2f;
    readonly Color EmissionBaseValue = Color.black;
    readonly Color EmissionActiveValue = new Color(0.25f, 0.25f, 0.25f);

    int _id;
    Transform _unitStore;
    EffectManager _effects;
    Unit _unit;
    int? _owner;
    bool _highlighed;

    bool _blockPrefabActive;
    bool _leafletGuyPrefabActive;
    GameObject _blockObject;
    GameObject _leafletGuyObject;

    #endregion

    #region Public properties

    /// <summary>
    /// The ID of the current sector.
    /// </summary>
    public int Id => _id;

    /// <summary>
    /// The stats of the current sector.
    /// </summary>
    public EffectManager Stats => _effects;

    /// <summary>
    /// Whether the PVC is contained in this sector.
    /// </summary>
    public bool HasPVC => Stats.HasEffect<EffectImpl.PVCEffect>();

    /// <summary>
    /// Whether the sector allows the PVC to be hidden on it.
    /// </summary>
    public bool AllowPVC => Landmark == null && Unit == null;

    /// <summary>
    /// The Unit occupying this sector.
    /// </summary>
    public Unit Unit
    {
        get { return _unit == null ? null : _unit; } // explictly return null to allow null-checking
        set
        {
            if (_unit != null)
                _unit.OnDeath -= Unit_OnDeath;

            if (value != null)
            {
                Sector prev = _unit?.Sector; // store previous sector

                _unit = value;
                _unit.transform.parent = _unitStore.transform;
                _unit.transform.localPosition = Vector3.zero;
                _unit.Sector = this;

                // if the target sector belonged to a different 
                // player than the unit, capture it and level up
                if (Owner != _unit.Owner)
                {
                    Owner = _unit.Owner;
                    _unit.LevelUp();
                    Debug.LogFormat("{0} captured sector", Owner);
                }

                OnUnitMove?.Invoke(_unit, new UpdateEventArgs<Sector>(prev, Unit.Sector));
                _unit.OnDeath += Unit_OnDeath;
            }
            else
                _unit = null;
        }
    }

    /// <summary>
    /// The player who owns the sector.
    /// </summary>
    public Player Owner
    {
        get { return _owner.HasValue ? Game.Instance.Players[_owner.Value] : null; }
        set
        {
            Player prev = Owner;
            _owner = value?.Id;

            // set sector color to the color of the given player
            // or gray if null
            gameObject.GetComponent<Renderer>().material.color = Owner?.Color ?? Color.gray;

            if (prev != Owner)
                OnCaptured?.Invoke(this, new UpdateEventArgs<Player>(prev, Owner));
        }
    }

    /// <summary>
    /// The neighbouring sectors.
    /// </summary>
    public IEnumerable<Sector> AdjacentSectors => m_adjacentSectors.Where(s => s.Stats.Traversable);

    /// <summary>
    /// The landmark on this sector.
    /// </summary>
    public Landmark Landmark => m_landmark;

    /// <summary>
    /// Gets or sets the highlight on the current sector.
    /// </summary>
    public bool Highlighted
    {
        get { return _highlighed; }
        set
        {
            GetComponent<Renderer>().material.SetColor("_EmissionColor", value ? EmissionActiveValue : EmissionBaseValue);
            _highlighed = value;
        }
    }

    /// <summary>
    /// Whether the sector block prefab is active.
    /// </summary>
    public bool BlockPrefabActive
    {
        get { return _blockPrefabActive; }
        set
        {
            if (value != _blockPrefabActive)
            {
                if (value)
                {
                    _blockObject = Instantiate(Game.Instance.Map.BlockSectorPrefab);
                    _blockObject.transform.parent = _unitStore.transform;
                    _blockObject.transform.localPosition = Vector3.zero;
                }
                else
                    Destroy(_blockObject);
                _blockPrefabActive = value;
            }
        }
    }

    /// <summary>
    /// Whether the leaflet guy prefab is active.
    /// </summary>
    public bool LeafletGuyPrefabActive
    {
        get { return _leafletGuyPrefabActive; }
        set
        {
            if (value != _leafletGuyPrefabActive)
            {
                if (value)
                {
                    _leafletGuyObject = Instantiate(Game.Instance.Map.LeafletGuyPrefab);
                    _leafletGuyObject.transform.parent = _unitStore.transform;
                    _leafletGuyObject.transform.localPosition = Vector3.zero;
                }
                else
                    Destroy(_leafletGuyObject);
                _leafletGuyPrefabActive = value;
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the sector is clicked.
    /// </summary>
    public event EventHandler OnClick;

    /// <summary>
    /// Raised when the sector is captured (i.e. when ownership changes).
    /// </summary>
    public event EventHandler<UpdateEventArgs<Player>> OnCaptured;

    /// <summary>
    /// Raised when a unit moves sector.
    /// </summary>
    public event EventHandler<UpdateEventArgs<Sector>> OnUnitMove;

    /// <summary>
    /// Raised when the unit in the sector dies.
    /// This event is a pure pass-through to reduce the need to dynamically
    /// re-bind events on unit create/move/death.
    /// </summary>
    public event EventHandler<EliminatedEventArgs> OnUnitDeath;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the sector with the given ID.
    /// </summary>
    /// <param name="id">The ID of the sector.</param>
    public void Init(int id)
    {
        _id = id;
    }

    #endregion

    #region Serialization

    public SerializableSector CreateMemento()
    {
        return new SerializableSector
        {
            effectManager = _effects.CreateMemento(),
            unit = Unit?.CreateMemento(),
            owner = _owner
        };
    }

    public void RestoreMemento(SerializableSector memento)
    {
        _owner = memento.owner;
        Owner = Owner; // if owned, apply ownership setup
        if (memento.unit != null)
        {
            // only sectors that have an owner can have a unit
            // this means we can shorthand unit intialization
            Owner.SpawnUnitAt(this); // spawn unit here
            Unit.RestoreMemento(memento.unit); // restore unit
        }
        _effects.RestoreMemento(memento.effectManager);
    }

    #endregion

    #region MonoBehaviour

    void Awake()
    {
        _unitStore = gameObject.transform.Find("Units");
        _effects = GetComponent<EffectManager>();
        _effects.Init(this);
        Owner = null;
        _unit = null;
    }

    internal void OnMouseUpAsButton() => OnClick?.Invoke(this, new EventArgs());

    #endregion

    #region Handlers

    void Unit_OnDeath(object sender, EliminatedEventArgs e)
    {
        _unit.OnDeath -= Unit_OnDeath;
        OnUnitDeath?.Invoke(sender, e);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets all the sectors around this one in the given range. Excludes the
    /// current sector.
    /// </summary>
    /// <returns>The sectors in range.</returns>
    /// <param name="range">The range to search.</param>
    public Sector[] GetSectorsInRange(int range)
    {
        HashSet<Sector> visited = new HashSet<Sector>();
        List<List<Sector>> fringe = new List<List<Sector>> { new List<Sector> { this } };
        for (int i = 1; i <= range; i++)
        {
            fringe.Add(new List<Sector>());
            foreach (Sector sector in fringe[i - 1])
                foreach (Sector adjacent in sector.AdjacentSectors)
                    if (adjacent != this && !visited.Contains(adjacent) && adjacent.Stats.Traversable)
                    {
                        visited.Add(adjacent);
                        fringe[i].Add(adjacent);
                    }
        }
        return visited.ToArray();
    }

    /// <summary>
    /// Swaps the units of the current and other sector.
    /// </summary>
    /// <param name="other">The sector to transfer the unit with.</param>
    public void TransferUnits(Sector other)
    {
        Unit tmp = Unit;
        Unit = other.Unit;
        other.Unit = tmp;
    }

    #endregion
}
