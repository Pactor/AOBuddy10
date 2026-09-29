namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System;

    /// <summary>
    /// What may be done with a container's contents.
    /// </summary>
    /// <remarks>
    /// The client keeps this on NewInventory_t at +0x24 and tests it a bit at a
    /// time, and the strings it prints when a bit is missing are what name the
    /// bits: 0x1004C0E5 tests bit 1 and complains Inv_DstCantAdd, 0x1004BFCE
    /// tests bit 2 and complains Inv_SrcCantRemove.
    ///
    /// Everything the client builds for itself is 3 - its own pages, a chest it
    /// owns, the corpse list. The overflow window at 0x1004B5F1 is 2, and so is
    /// every captured corpse: a corpse can be emptied and cannot be filled.
    /// </remarks>
    [Flags]
    public enum InventoryAccess
    {
        /// <summary>
        /// Nothing may be moved either way.
        /// </summary>
        None = 0,

        /// <summary>
        /// Items may be put in.
        /// </summary>
        CanAdd = 1,

        /// <summary>
        /// Items may be taken out.
        /// </summary>
        CanRemove = 2
    }
}
