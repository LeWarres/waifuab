using System.Collections.Generic;
using UnityEngine;

public partial class TrainSim
{
    bool gabrielInterlude;
    int gabrielDraft;
    bool gabrielResumeDuel;
    readonly List<string> gabrielOffers=new();
    public bool GabrielInterludeOpen => gabrielInterlude;
    public int GabrielDraft => gabrielDraft;

    void GabrielInterlude()
    {
        gabrielInterlude=true;duel=false;chapterWon=false;
        StoryIntro.Inline(()=>
        {
            gabrielInterlude=false;gabrielDraft=1;gabrielOffers.Clear();
            UiKit.SetBiome(BiomeId);Time.timeScale=0f;
        });
    }

    void BuildGabrielDraft()
    {
        menuTitle=L10n.T(gabrielDraft==1 ? "gabriel.upgrade.weapon" : "gabriel.upgrade.passive");
        menuSub=L10n.T("gabriel.upgrade.hint");
        if(gabrielOffers.Count==0)
        {
            var pool=new List<string>();
            if(gabrielDraft==1)
            {
                foreach(var weapon in Weapon.Wagon)
                {
                    bool maxed=cars.Exists(c=>c.mount && c.mount.Weapon==weapon && c.mount.Level>=Weapon.MaxLevel);
                    bool room=cars.Count<maxCars || cars.Exists(c=>c.cargo<0&&!c.mount);
                    bool owned=cars.Exists(c=>c.mount&&c.mount.Weapon==weapon);
                    if(!maxed && (room||owned))pool.Add("wagon:"+weapon.id);
                }
                if(Player.level<Weapon.MaxLevel)pool.Add("evolve");
            }
            else foreach(string id in Passives)
                if(PassiveLevel(id)<PassiveCap(id) && (PassiveLevel(id)>0 || passiveOrder.Count<MaxOwnedPassives))pool.Add(id);
            while(pool.Count>0 && gabrielOffers.Count<3){int i=Random.Range(0,pool.Count);gabrielOffers.Add(pool[i]);pool.RemoveAt(i);}
            if(gabrielOffers.Count==0)gabrielOffers.Add("repair");
        }
        foreach(string offered in gabrielOffers)
        {
            string token=offered;
            bool wagon=token.StartsWith("wagon:");
            var weapon=wagon ? System.Array.Find(Weapon.Wagon,w=>w.id==token.Substring(6)) : null;
            string name=weapon!=null ? weapon.Name : token=="evolve" ? Player.ammo.Name : L10n.T("passive."+token);
            string label=token=="repair" ? L10n.T("gabriel.upgrade.repair") : L10n.T("gabriel.upgrade.take",name);
            int passive=System.Array.IndexOf(Passives,token);
            string icon=token=="repair" ? "health" : wagon ? token.Substring(6) : token=="evolve" ? Player.ammo.id : passive>=0 ? PassiveIcons[passive] : token;
            Add(icon,label,()=>PickGabrielUpgrade(token));
        }
    }

    void PickGabrielUpgrade(string token)
    {
        if(gabrielDraft==0)return;
        if(token=="repair"){Repair(wagonMaxHealth);Player.Heal(Player.maxHealth);}
        else {Apply(token);Took(token);}
        gabrielOffers.Clear();
        if(gabrielDraft==1){gabrielDraft=2;SaveGame.SaveGabrielBuild(taken,2);return;}
        gabrielDraft=0;SaveGame.SaveGabrielBuild(taken,0);BeginGabrielDuel();
    }

    void BeginGabrielDuel()
    {
        duelCalled=true;finaleRetry=false;finaleLosses=0;
        Story.Duel(true,out string[] names,out string[] lines,out bool[] speakers);
        talk.Show(names,lines,speakers,StartFinale);
    }
}
