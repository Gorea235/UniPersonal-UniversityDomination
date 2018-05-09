using System;
using System.Linq;

namespace EffectImpl
{
    [Serializable]
    public class PlayerSkipTurnEffect : Effect
    {
        #region Override Properties

        public override string CardName => "Summer break";

        public override string CardDescription => "Target player skips a turn";

        public override CardCornerIcon CardCornerIcon => CardCornerIcon.EnemyPlayer;

        public override CardTier CardTier => CardTier.Tier2;

        #endregion

        #region Concrete Methods

        public override EffectAvailableSelection AvailableSelection(Game game) => new EffectAvailableSelection
        {
            Players = game.Players.Where(p => p != game.CurrentPlayer && !p.SkipNextTurn)
        };
        
        #endregion

        #region Helper Methods

        protected override void ApplyToPlayer()
        {
			AppliedPlayer.SkipNextTurn = true;
		}

        #endregion
    }
}
