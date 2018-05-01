﻿using System.Collections;
using System.Linq;
using UnityEngine;

public class HumanPlayer : Player
{
    #region Private Fields

    Sector _selectedSector;
    Sector[] _sectorsInSelectedRange;

    #endregion

    #region Public Properties

    public override PlayerKind Kind => PlayerKind.Human;

    #endregion

    #region Override Methods

    public override void ProcessTurnStart()
    {
        StartCoroutine(DoStartTurn());
    }

    public override void ProcessSectorClick(Sector clickedSector)
    {
        base.ProcessSectorClick(clickedSector);
        if (CanPerformActions && _selectedSector == null)
        {
            if (clickedSector.Unit != null && clickedSector.Unit.Owner == this && clickedSector.Unit.Stats.CanMove)
                SelectSector(clickedSector);
        }
        else
        {
            if (CanPerformActions && _selectedSector.Unit.SectorsInRange.Contains(clickedSector))
                AttemptMove(_selectedSector, clickedSector);
            DeselectSector();
        }
    }

    public override void EndTurn()
    {
        StartCoroutine(DoEndTurn());
    }

    #endregion

    #region Helper Methods

    void SelectSector(Sector sector)
    {
        _selectedSector = sector;
        _sectorsInSelectedRange = _selectedSector.Unit.SectorsInRange;
        Game.Instance.Map.ApplySectorHighlight(true, _sectorsInSelectedRange);
    }

    public void DeselectSector()
    {
        Game.Instance.Map.ApplySectorHighlight(false, Game.Instance.Map.Sectors);
        _selectedSector = null;
    }

    IEnumerator DoStartTurn()
    {
        base.ProcessTurnStart();
        Cards.CardsEnter();
        if (Cards.Count > 0)
            yield return new WaitForSeconds(1.5f);
        if (HasHadTurn)
        {
            AssignRandomCard();
            yield return new WaitForSeconds(0.5f);
        }
        Game.Instance.EndTurnButtonEnabled = true;
    }

    IEnumerator DoEndTurn()
    {
        Game.Instance.EndTurnButtonEnabled = false;
        Cards.CardsExit();
        if (Cards.Count > 0)
            yield return new WaitForSeconds(1.5f);
        base.EndTurn();
    }

    #endregion
}
