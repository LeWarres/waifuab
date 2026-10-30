from pathlib import Path

def edit(path, changes):
    p = Path(path)
    text = p.read_text(encoding='utf-8-sig')
    for before, after in changes:
        assert text.count(before) == 1, (path, before[:100], text.count(before))
        text = text.replace(before, after)
    p.write_text(text, encoding='utf-8')

edit('Assets/Scripts/Story.cs', [
    ('drawings, holo; }', 'drawings, holo, circuits, morality; }'),
    ('    public static Part[] Route(string hero)', '''    static readonly Part[] Golem =
    {
        new Part { biome=9, rival=3, intro=6, outro=5, before="rhr", after="hr" },
        new Part { biome=6, rival=10, intro=2, outro=4, before="rhr", after="rh" },
        new Part { biome=13, rival=11, intro=2, outro=4, before="hh", after="h", circuits=true },
        new Part { biome=14, rival=4, intro=3, outro=6, before="hh", after="h", morality=true },
    };

    public static Part[] Route(string hero)'''),
    ('"tzmil" => Tzmil, _ => null', '"tzmil" => Tzmil, "sentinel" => Golem, _ => null'),
    ('textKey = $"story.{Hero}.{ending}.outro.{n}"', 'textKey = Hero=="sentinel" && ending==3 ? $"story.sentinel.3.{(SaveGame.GolemHighMorality ? "good" : "bad")}.{n}" : $"story.{Hero}.{ending}.outro.{n}"'),
    ('Hero == "tzmil" ? TzmilArt(ending, false, n) : null', 'Hero == "tzmil" ? TzmilArt(ending, false, n) : Hero == "sentinel" ? GolemArt(ending, false, n) : null'),
    ('Hero == "tzmil" ? TzmilArt(Chapter, true, n) : null', 'Hero == "tzmil" ? TzmilArt(Chapter, true, n) : Hero == "sentinel" ? GolemArt(Chapter, true, n) : null'),
    ('        // Faceless enters Earth', '''        // Golem uses the existing ascent before his first space chapter, including retry narration.
        if (Hero=="sentinel" && Chapter==1 && !ascentPassed)
        {
            InspectionPending=false;AscentPending=true;
            if(pages.Count==0)for(int n=1;n<=route[0].outro;n++)
                pages.Add(new StoryIntro.Page {textKey=$"story.sentinel.0.outro.{n}",titleKey="story.sentinel.0.title",number=1,artwork=GolemArt(0,false,n)});
            return pages.ToArray();
        }
        // Faceless enters Earth'''),
    ('    // Upgrades as tokens,', '''    public static string GolemArt(int chapter, bool opening, int n)
    {
        string name=opening ? chapter switch
        {
            0=>n<=3 ? "origin" : "orders",
            1=>"faceless",
            2=>n==1 ? "portal" : "circuits",
            _=>n<=2 ? "heaven" : "judgment"
        } : chapter switch
        {
            0=>n==1 ? "tzmil" : n==2 ? "vent" : n==3 ? "clones" : "launch",
            1=>n==1 ? "faceless" : "portal",
            2=>n<=2 ? "circuits" : "sacrifice",
            _=>SaveGame.GolemHighMorality ? (n<=3 ? "peace" : "rebirth") : (n<=3 ? "prison" : "baratito")
        };
        return "Story/golem_"+name;
    }

    // Upgrades as tokens,'''),
])
edit('Assets/Scripts/SaveGame.cs', [
    ('        public bool story;', '        public int golemMoralAnswers;\n        public bool story;'),
    ('        D.chapter = D.reached = 0;', '        D.chapter = D.reached = 0;\n        D.golemMoralAnswers = 0;'),
    ('    public static int Tickets =>', '''    public static bool GolemHighMorality => D.golemMoralAnswers >= 7;
    public static void GolemJudged(int highAnswers) { D.golemMoralAnswers = Mathf.Clamp(highAnswers, 0, 13); Write(); }

    public static int Tickets =>'''),
])
edit('Assets/Scripts/TrainSim.cs', [
    ('    void StartFinale()\n    {', '''    void StartFinale()
    {
        if(Story.Current.circuits || Story.Current.morality)
        {
            golemChallenge ??= gameObject.AddComponent<GolemChallenge>();
            golemChallenge.Begin(Story.Current.morality,()=>
            {
                Story.Duel(false,out string[] names,out string[] lines,out bool[] speakers);
                talk.Show(names,lines,speakers,()=>RaceFinished(true));
            });
            return;
        }'''),
    ('    public bool Racing =>', '    GolemChallenge golemChallenge;\n    public bool Racing => (golemChallenge && golemChallenge.Active) ||'),
    ('            else if (holo.Active) holo.TestWin();', '            else if (holo.Active) holo.TestWin();\n            else if (golemChallenge && golemChallenge.Active) golemChallenge.TestComplete();'),
    ('talk.Show(names, lines, rival, Story.Current.holo ? StartFinale', 'talk.Show(names, lines, rival, Story.Current.holo || Story.Current.circuits || Story.Current.morality ? StartFinale'),
    ('        ("thulu",new Color(.12f,.2f,.19f),new Color(.37f,.55f,.39f)),', '        ("thulu",new Color(.12f,.2f,.19f),new Color(.37f,.55f,.39f)),\n        ("robotheaven",new Color(.82f,.9f,.95f),new Color(.94f,.79f,.42f)),'),
    ('        (2, new Color(.25f,.35f,.3f),', '        (2, new Color(.92f,.94f,.97f), new Color(.56f,.73f,.85f), new Color(1f,.68f,.2f), 1.5f), // robotic heaven: white ceramic / gold circuit rails\n        (2, new Color(.25f,.35f,.3f),'),
])
# Keep biome/track indices aligned: the new track belongs after Thulu.
p=Path('Assets/Scripts/TrainSim.cs');t=p.read_text(encoding='utf-8');line='        (2, new Color(.92f,.94f,.97f), new Color(.56f,.73f,.85f), new Color(1f,.68f,.2f), 1.5f), // robotic heaven: white ceramic / gold circuit rails\n';t=t.replace(line,'');needle=' // Thulu World: a causeway inside a cosmic god\n';assert t.count(needle)==1;t=t.replace(needle,needle+line);p.write_text(t,encoding='utf-8')
edit('Assets/Scripts/BossEvent.cs', [('        new Boss { id="thulu",accent=new Color(.45f,.9f,.65f),healthMul=2.8f },','        new Boss { id="thulu",accent=new Color(.45f,.9f,.65f),healthMul=2.8f },\n        new Boss { id="robotheaven",accent=new Color(1f,.72f,.25f),healthMul=2.8f },')])
edit('Assets/Scripts/StationMenu.cs', [('        "thulu" => new Tones', '        "robotheaven" => new Tones { main=new(.95f,.62f,.16f),deep=new(.06f,.12f,.21f),hot=new(.45f,.9f,1f) },\n        "thulu" => new Tones')])
edit('Assets/Scripts/Ascenso.cs', [
    ('string ArrivalKey => descending ? "descent.arrived" : "ascent.arrived";', 'string ArrivalKey => Story.Active && Story.Hero=="sentinel" ? "story.sentinel.ascent.arrived" : descending ? "descent.arrived" : "ascent.arrived";'),
    ('ShowDialog(descending ? "descent.intro" : "ascent.intro",', 'ShowDialog(Story.Active && Story.Hero=="sentinel" ? "story.sentinel.ascent.intro" : descending ? "descent.intro" : "ascent.intro",'),
])
print('Golem route, ascent, endings, challenge hooks and robotic-heaven biome connected.')
