while (true)
{
    string name = ColoredConsole.Prompt("What is your name?").ToUpper();

    Console.WriteLine("Select option: ");
    Console.WriteLine("1 - Computer VS. Computer");
    Console.WriteLine("2 - Human    VS. Computer");
    Console.WriteLine("3 - Human    VS. Human");
    string choice = ColoredConsole.Prompt("What mode do you want to use?");

    IPlayer player1, player2;

    if (choice == "1") { player1 = new ComputerPlayer(); player2 = new ComputerPlayer(); }
    else if (choice == "2") { player1 = new PlayerConsole(); player2 = new ComputerPlayer(); }
    else { player1 = new PlayerConsole(); player2 = new PlayerConsole(); }

    int round = 0;

    Party heroes = new Party(player1);
    heroes.Characters.Add(new Fletcher());
    heroes.Characters.Add(new TheTrueProgrammer(name));

    heroes.Items.Add(new HealItem());
    heroes.Items.Add(new HealItem());
    heroes.Items.Add(new HealItem());

    heroes.Items.Add(new Soup());

    List<Party> battleRound = new List<Party>() { CreateMonsterParty1(player2), CreateMonsterParty2(player2), CreateMonsterParty3(player2) };

    for (int battleNumber = 0; battleNumber < battleRound.Count; battleNumber++)
    {
        round++;
        ColoredConsole.WriteLine($"Round: {round}", ConsoleColor.DarkMagenta);
        Party monsters = battleRound[battleNumber];
        monsters.Items.Add(new HealItem());
        Battle battle = new Battle(heroes, monsters);
        battle.Run();

        if (heroes.Characters.Count == 0) break;
    }

    if (heroes.Characters.Count > 0) ColoredConsole.WriteLine("You win! The Uncoded One has been defeated.", ConsoleColor.Green);
    else ColoredConsole.WriteLine("You lose! The forces of The Uncoded One have prevailed.", ConsoleColor.Red);

    string again = ColoredConsole.Prompt("Do you want to play again(yes/no)?").ToLower().Trim();
    if (again != "yes") return;
}

Party CreateMonsterParty1(IPlayer playerControl)
{
    Party monster = new Party(playerControl);
    monster.Characters.Add(new Skeleton());
    return monster;
}

Party CreateMonsterParty2(IPlayer playerControl)
{
    Party monsters = new Party(playerControl);
    monsters.Characters.Add(new Skeleton());
    monsters.Characters.Add(new Skeleton());
    return monsters;
}

Party CreateMonsterParty3(IPlayer playerControl)
{
    Party monsters = new Party(playerControl);
    monsters.Characters.Add(new FlameWraith());
    monsters.Characters.Add(new Boss());
    return monsters;
}

public class Battle
{
    public Party Heroes { get; }
    public Party Monsters { get; }
    private GameRendering BattleRenderer { get; } = new GameRendering();

    public Battle(Party heroes, Party monsters)
    {
        Heroes = heroes;
        Monsters = monsters;
    }

    public void Run()
    {
        while (true)
        {
            foreach (Party party in new[] { Heroes, Monsters })
            {
                foreach (Character character in party.Characters)
                {
                    Console.WriteLine();

                    BattleRenderer.Rendering(this, character);

                    Console.WriteLine($"It is {character.Name}’s turn...");
                    party.Player.ChooseAction(this, character).Run(this, character);

                    if (IsOver()) break;
                }
            }
            if (IsOver()) break;
        }
    }

    private bool IsOver() => Heroes.Characters.Count == 0 || Monsters.Characters.Count == 0;

    public Party GetPartyFor(Character character) => Heroes.Characters.Contains(character) ? Heroes : Monsters;
    public Party GetEnemyPartyFor(Character character) => Heroes.Characters.Contains(character) ? Monsters : Heroes;
}


public class GameRendering
{
    public void Rendering(Battle battle, Character activeCharacter)
    {

        Console.WriteLine("============================================= BATTLE ============================================");
        foreach (Character character in battle.Heroes.Characters)
        {
            ConsoleColor color = character == activeCharacter ? ConsoleColor.Yellow : ConsoleColor.Gray;

            if (character.HP / (float)character.MaxHp < 0.10)
                ColoredConsole.WriteLine($"{character.Name} ({character.HP}/{character.MaxHp}) !", ConsoleColor.Red);
            else Console.WriteLine($"{character.Name} ({character.HP}/{character.MaxHp}) ");
        }
        Console.WriteLine("---------------------------------------------- VS -----------------------------------------------");
        foreach (Character character in battle.Monsters.Characters)
        {
            ConsoleColor color = character == activeCharacter ? ConsoleColor.Yellow : ConsoleColor.Gray;
            if (character.HP / (float)character.MaxHp < 0.10)
                ColoredConsole.WriteLine($"                                                          {character.Name,45} ({character.HP}/{character.MaxHp}) !", ConsoleColor.Red);
            else ColoredConsole.WriteLine($"                                                         {character.Name,45} ({character.HP}/{character.MaxHp})", color);
        }
        Console.WriteLine("=================================================================================================");
    }
}

public interface IAction
{
    void Run(Battle battle, Character actor);
}

public record DataAttack(int Damage, double ProbabilitySucess);

public class DoNothing : IAction
{
    public void Run(Battle battle, Character actor) => Console.WriteLine($"{actor.Name} do NOTHING");
}

public class AttackAction : IAction
{
    private readonly Attack _attack;
    private readonly Character _target;

    private static readonly Random _random = new Random();

    public AttackAction(Attack attack, Character target)
    {
        _attack = attack;
        _target = target;
    }

    public void Run(Battle battle, Character character)
    {
        DataAttack data = _attack.Create();

        if (_random.NextDouble() > data.ProbabilitySucess)
        {
            ColoredConsole.WriteLine($"{character.Name} MISSED!", ConsoleColor.DarkRed);
            return;
        }

        Console.WriteLine($"{character.Name} used {_attack.Name} on {_target.Name}");

        Console.WriteLine($"{_attack.Name} dealt {data.Damage} damage to {_target.Name}.");
        Console.WriteLine($"{_target.Name} is now at {_target.HP}/{_target.MaxHp} HP.");

        _target.HP -= data.Damage;

        if (!_target.IsAlive)
        {
            battle.GetPartyFor(_target).Characters.Remove(_target);
            Console.WriteLine($"{_target.Name} has been defeated!");
        }
    }
}

public class UseItem : IAction
{
    private readonly IItem _item;

    public UseItem(IItem item) => _item = item;

    public void Run(Battle battle, Character character)
    {
        _item.Use(battle, character);
        Console.WriteLine($"{_item.Name} used on {character.Name}");

        battle.GetPartyFor(character).Items.Remove(_item);
    }
}

public interface Attack
{
    string Name { get; }
    DataAttack Create();
}

public class BurningClaw : Attack
{
    public string Name => "Burning Claw";
    public DataAttack Create() => new DataAttack(2, 1);
}

public class Unraveling : Attack
{
    private static readonly Random _random = new Random();
    public string Name => "UNRAVELING";
    public DataAttack Create() => new DataAttack(_random.Next(3), 1);
}

public class Punch : Attack
{
    public string Name => "PUNCH";
    public DataAttack Create() => new DataAttack(1, 1);
}

public class BoneCrunch : Attack
{
    private static readonly Random _random = new Random();
    public string Name => "BONE CRUNCH";
    public DataAttack Create() => new DataAttack(_random.Next(2), 1);
}

public class QuickShot : Attack
{
    public string Name => "QUICK SHOT";
    public DataAttack Create() => new DataAttack(3, 0.5);
}


public interface IPlayer
{
    IAction ChooseAction(Battle battle, Character character);
}

public class PlayerConsole : IPlayer
{
    public IAction ChooseAction(Battle battle, Character character)
    {
        List<MenuChoice> menuChoice = CreateMenuChoice(battle, character);

        for (int index = 0; index < menuChoice.Count; index++)
        {
            Console.WriteLine($"{index + 1} - {menuChoice[index].Decriptions}", menuChoice[index].IsEnable ? ConsoleColor.Gray : ConsoleColor.DarkGray);
        }

        string choice = ColoredConsole.Prompt("What do you want to do?");
        int indexMenu = Convert.ToInt32(choice) - 1;

        if (menuChoice[indexMenu].IsEnable) return menuChoice[indexMenu].Action!;
        return new DoNothing();
    }

    private List<MenuChoice> CreateMenuChoice(Battle battle, Character character)
    {
        Party currentParty = battle.GetPartyFor(character);
        Party otherParty = battle.GetEnemyPartyFor(character);

        List<MenuChoice> menuChoices = new List<MenuChoice>();

        if (otherParty.Characters.Count > 0)
            menuChoices.Add(new MenuChoice($"Standard Attack {character.StandardAttack.Name}", new AttackAction(character.StandardAttack, otherParty.Characters[0])));
        else
            menuChoices.Add(new MenuChoice($"Standard Attack {character.StandardAttack.Name}", null));

        if (currentParty.Items.Count > 0)
            menuChoices.Add(new MenuChoice($"Potion use ({currentParty.Items[0].Name}, {currentParty.Items.Count})", new UseItem(battle.GetPartyFor(character).Items[0])));
        else
            menuChoices.Add(new MenuChoice($"Potion use: (0)", null));

        menuChoices.Add(new MenuChoice("Do Nothing", new DoNothing()));

        return menuChoices;
    }
}

public record MenuChoice(string Decriptions, IAction? Action)
{
    public bool IsEnable => Action != null;
}

public class ComputerPlayer : IPlayer
{
    private static readonly Random _random = new Random();
    public IAction ChooseAction(Battle battle, Character character)
    {
        //Thread.Sleep(500);

        List<Character> potentialTargets = battle.GetEnemyPartyFor(character).Characters;
        if (potentialTargets.Count > 0) return new AttackAction(character.StandardAttack, battle.GetEnemyPartyFor(character).Characters[0]);

        bool HasPotion = battle.GetPartyFor(character).Items.Count > 0;
        bool isHPUnderThreshold = character.HP / (float)character.MaxHp < 0.25;

        if (HasPotion && isHPUnderThreshold && _random.NextDouble() < 0.5)
            return new UseItem(battle.GetPartyFor(character).Items[0]);

        return new DoNothing();
    }
}

public interface IItem
{
    string Name { get; }
    void Use(Battle battle, Character user);
}

public class HealItem : IItem
{
    public string Name => "HEALTH POTION";
    public void Use(Battle battle, Character user)
    {
        user.HP += 10;
        Console.WriteLine($"{user.Name} has been increased by 10.");

        if (user.HP > user.MaxHp)
            user.HP = user.MaxHp;
    }
}

public class Soup : IItem
{
    public string Name => "SOUP";
    public void Use(Battle battle, Character user)
    {
        user.HP = user.MaxHp;
        Console.WriteLine($"{user.Name}'s HP has been fully restored!");
    }
}

public abstract class Character
{
    public abstract string Name { get; }
    public abstract Attack StandardAttack { get; }
    private int _hp;
    public int MaxHp { get; }
    public int HP
    {
        get => _hp;
        set => _hp = Math.Clamp(value, 0, MaxHp);
    }

    public bool IsAlive => HP > 0;

    public Character(int hp)
    {
        MaxHp = hp;
        HP = hp;
    }
}

public class TheTrueProgrammer : Character
{
    public override string Name { get; }
    public override Attack StandardAttack { get; } = new Punch();
    public TheTrueProgrammer(string name) : base(25) => Name = name;
}

public class Fletcher : Character
{
    public override string Name => "FLETCHER";
    public override Attack StandardAttack { get; } = new QuickShot();
    public Fletcher() : base(5) { }
}

public class Boss : Character
{
    public override Attack StandardAttack { get; } = new Unraveling();
    public override string Name => "BOSS";
    public Boss() : base(25) { }
}

public class FlameWraith : Character
{
    public override Attack StandardAttack { get; } = new BurningClaw();
    public override string Name => "FLAME WRAITH";
    public FlameWraith() : base(5) { }
}

public class Skeleton : Character
{
    public override Attack StandardAttack { get; } = new BoneCrunch();

    public override string Name => "SKELETON";
    public Skeleton() : base(5) { }
}

public class Party
{
    public List<Character> Characters { get; } = new List<Character>();
    public List<IItem> Items { get; } = new List<IItem>();
    public IPlayer Player { get; }

    public Party(IPlayer player)
    {
        Player = player;
    }
}

public static class ColoredConsole
{
    public static void WriteLine(string text, ConsoleColor color)
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = previousColor;
    }
    public static void Write(string text, ConsoleColor color)
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previousColor;
    }

    public static string Prompt(string questiontoAsk)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.Write(questiontoAsk + " ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        string input = Console.ReadLine() ?? "...";
        Console.ForegroundColor = previousColor;
        return input;
    }
}
