using System;

namespace Felix2D
{
    // Engine-independent rules, shared by the actual Unity player and editor checks.
    public sealed class GameState
    {
        public const int Columns = 3, Floors = 4;
        public readonly bool[,] Broken = new bool[Columns, Floors];
        public int Column { get; private set; }
        public int Floor { get; private set; }
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Level { get; private set; }
        public float TimeLeft { get; private set; }
        public float Protection { get; private set; }
        public bool Over { get { return Lives <= 0 || TimeLeft <= 0; } }
        public int Remaining { get { int n=0; foreach(bool b in Broken) if(b) n++; return n; } }
        public GameState() { Restart(); }
        public void Restart() { Column=1; Floor=0; Score=0; Lives=3; Level=1; TimeLeft=60; Protection=0; ResetWindows(); }
        void ResetWindows() { for(int c=0;c<Columns;c++) for(int f=0;f<Floors;f++) Broken[c,f]=true; }
        public void Move(int dc,int df) { if(Over)return; Column=Math.Max(0,Math.Min(Columns-1,Column+dc)); Floor=Math.Max(0,Math.Min(Floors-1,Floor+df)); }
        public bool Repair() {
            if(Over || !Broken[Column,Floor])return false;
            Broken[Column,Floor]=false; Score+=100;
            if(Remaining==0) { Level++; TimeLeft+=20; ResetWindows(); Protection=Math.Max(Protection,1.1f); }
            return true;
        }
        public void DamageWindow(int c,int f) { if(!Over && c>=0 && c<Columns && f>=0 && f<Floors) Broken[c,f]=true; }
        public bool Hit() { if(Over || Protection>0)return false; Lives--; Protection=1.1f; return true; }
        public void Tick(float seconds) { if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds))throw new ArgumentOutOfRangeException("seconds"); if(Over)return; TimeLeft=Math.Max(0,TimeLeft-seconds); Protection=Math.Max(0,Protection-seconds); }
    }
}
