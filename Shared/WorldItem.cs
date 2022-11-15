﻿//UOLib/UWorldItem.pas

namespace Shared;

//TWorldItem
public abstract class WorldItem : MulBlock, IComparable<WorldItem> {
    protected bool _locked;
    protected WorldBlock _owner;
    protected bool _selected;

    protected ulong _tileId;
    protected ulong _x;
    protected ulong _y;
    protected short _z;

    public WorldItem(WorldBlock owner) {
        Selected = false;
        Locked = false;
        Owner = owner;
    }

    public WorldBlock Owner {
        get => _owner;
        set {
            if (_owner != value) {
                if (_owner != null) {
                    Owner.Changed = true;
                    if (Locked) Owner.RemoveRef();
                    if (Selected) Owner.RemoveRef();
                }

                _owner = value;
                if (_owner != null) {
                    Owner.Changed = true;
                    if (Locked) Owner.AddRef();
                    if (Selected) Owner.AddRef();
                }
            }
        }
    }

    public virtual ulong TileId {
        get => RawTileId;
        set {
            if (RawTileId != value) {
                RawTileId = value;
                DoChanged();
            }
        }
    }

    public ulong X {
        get => _x;
        set {
            if (_x != value) {
                _x = value;
                DoChanged();
            }
        }
    }

    public ulong Y {
        get => _y;
        set {
            if (_y != value) {
                _y = value;
                DoChanged();
            }
        }
    }

    public virtual short Z {
        get => RawZ;
        set {
            if (RawZ != value) {
                RawZ = value;
                DoChanged();
            }
        }
    }

    public bool Selected {
        get => _selected;
        set {
            if (Owner != null && _selected != value) {
                if (value) Owner.AddRef();
                else Owner.RemoveRef();
            }

            _selected = value;
        }
    }

    public bool CanBeEdited { get; set; }

    public bool Locked {
        get => _locked;
        set {
            if (_locked != value) {
                _locked = value;
                if (Owner != null) {
                    if (_locked)
                        Owner.AddRef();
                    else
                        Owner.RemoveRef();
                }
            }
        }
    }

    public int Priority { get; set; }
    public short PriorityBonus { get; set; }
    public int PrioritySolver { get; set; }

    public ulong RawTileId {
        get => _tileId;
        private set => _tileId = value;
    }

    public short RawZ {
        get => _z;
        private set => _z = value;
    }

    //Can we replace some with autogenerated one?
    public int CompareTo(WorldItem? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        if (X != other.X) return (int)(X - other.X);
        if (Y != other.Y) return (int)(Y - other.Y);
        var result = Priority.CompareTo(other.Priority);
        if (result != 0) {
            if (this is MapCell && other is StaticItem) return -1;
            if (this is StaticItem && other is MapCell) return 1;
            if (this is MapCell && other is VirtualTile) return -1;
            if (this is VirtualTile && other is MapCell) return 1;
        }

        return PrioritySolver - other.PrioritySolver;
    }

    protected void DoChanged() {
        if (Owner != null) Owner.Changed = true;
    }

    public void UpdatePos(ulong x, ulong y, short z) {
        _x = x;
        _y = y;
        RawZ = z;
        DoChanged();
    }

    public void Delete() {
        Selected = false;
        Locked = false;
        DoChanged();
    }
}