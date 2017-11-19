using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Braille;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Braille
/// Created by Timwi
/// </summary>
public class BrailleModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public Mesh[] BrailleLetterMeshes;
    public MeshFilter[] BrailleLetterHighlights;
    public KMSelectable[] BrailleLetterSelectables;

    private sealed class BrailleLetter
    {
        public string Bit;
        public bool CanBeInitial;
        public bool CanBeFinal;
        public int Dots;
    }

    private Dictionary<string, BrailleLetter> _brailleLetters;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved;

    private static Dictionary<string, int> _solutions = @"
acting=3,dating=4,heading=1,meaning=3,server=4
aiming=1,dealer=3,hearing=1,miners=4,shaking=1
artist=2,eating=2,heating=2,nearer=1,sought=1
asking=4,eighth=2,higher=2,parish=4,staying=1
bearing=4,farmer=4,insist=3,parker=4,strands=2
beating=3,farming=2,lasted=3,parking=1,strings=4
beings=1,faster=1,laying=2,paying=4,teaching=1
binding=2,father=1,leader=4,powers=1,tended=4
bought=4,finding=1,leading=4,pushed=1,tender=1
boxing=4,finest=3,leaned=4,pushing=2,testing=3
breach=2,finish=4,leaning=4,rather=3,throwing=3
breast=1,flying=2,leaving=1,reaching=3,towers=4
breath=3,foster=2,linking=1,reader=1,vested=3
breathe=3,fought=3,listed=2,reading=1,warned=3
bringing=3,gaining=3,listen=1,resting=3,warning=2
brings=3,gather=4,living=4,riding=2,weaker=3
carers=2,gazing=4,making=3,rushed=2,wealth=2
carter=3,gender=4,marked=1,rushing=1,winner=2
charter=2,growing=4,marking=1,saying=2,winning=3
crying=4,headed=2,master=2,served=2,winter=3".Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Split('=')).ToDictionary(arr => arr[0], arr => int.Parse(arr[1]) - 1);

    private string _word;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;

        _brailleLetters = @"a=1 b=12 c=14 d=145 e=15 f=124 g=1245 h=125 i=24 j=245
k=13 l=123 m=134 n=1345 o=135 p=1234 q=12345 r=1235 s=234 t=2345 u=136 v=1236 x=1346 y=13456 z=1356
and=12346 for=123456 of=12356 the=2346 with=23456
ch=16 gh=126 sh=146 th=1456 wh=156 ed=1246 er=12456 ou=1256 ow=246 w=2456
-ea-=2 -bb-=23 -cc-=25 en=26 -ff-=235 -gg-=2356 in=35 st=34 ar=345 -ing=346"
            .Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(bit => Regex.Match(bit, @"^(-)?(\w+)(-)?=(\d+)$"))
            .Where(m => m.Success)
            .Select(m => new BrailleLetter
            {
                Bit = m.Groups[2].Value,
                CanBeInitial = !m.Groups[1].Success,
                CanBeFinal = !m.Groups[3].Success,
                Dots = m.Groups[4].Value.Aggregate(0, (prev, next) => prev | (1 << (next - '1')))
            })
            .ToDictionary(inf => inf.Bit);

        for (int i = 0; i < 4; i++)
            BrailleLetterSelectables[i].OnInteract = GetHandler(i);

        Invoke("SetFirstWord", 0.1f);
    }

    private KMSelectable.OnInteractHandler GetHandler(int i)
    {
        return delegate
        {
            BrailleLetterSelectables[i].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, BrailleLetterSelectables[i].transform);

            if (_isSolved)
                return false;

            if (_solutions[_word] == i)
            {
                Debug.LogFormat("[Braille #{0}] Correct letter pressed.", _moduleId);
                Module.HandlePass();
                _isSolved = true;

                for (int j = 0; j < 4; j++)
                {
                    BrailleLetterHighlights[j].mesh = null;
                    var hClone = BrailleLetterHighlights[j].transform.Find("Highlight(Clone)");
                    if (hClone != null)
                        hClone.GetComponent<MeshFilter>().mesh = null;
                }
            }
            else
            {
                Debug.LogFormat("[Braille #{0}] Wrong letter pressed: {1}", _moduleId, i + 1);
                Module.HandleStrike();
                SetWord(false);
            }

            return false;
        };
    }

    private string convert(int[] dots)
    {
        var str = new StringBuilder();
        for (int i = 0; i < dots.Length; i++)
        {
            for (int j = 0; j < 6; j++)
                str.Append((dots[i] & (1 << j)) != 0 ? '■' : '·');
            str.Append('/');
        }
        return str.ToString().Substring(0, str.Length - 1);
    }

    private void SetFirstWord()
    {
        SetWord(true);
    }

    private void SetWord(bool first)
    {
        var brailleRegex = new Regex(@"({0})".Fmt(_brailleLetters.OrderByDescending(kvp => kvp.Key.Length).Select(kvp => "{0}{1}{2}".Fmt(kvp.Value.CanBeInitial ? "" : "(?!^)", kvp.Key, kvp.Value.CanBeFinal ? "" : "(?!$)")).JoinString("|")));

        var words = _solutions.Keys.ToArray();

        tryAgain:
        var flippedPositions = new List<int>();
        _word = words[Rnd.Range(0, words.Length)];
        var braille = brailleRegex.Matches(_word).Cast<Match>().Select(m => _brailleLetters[m.Value].Dots).ToArray();
        var origPatterns = Enumerable.Range(0, 4).Select(ix => Enumerable.Range(0, 6).Select(cell => (braille[ix] & (1 << cell)) == 0 ? null : (cell + 1).ToString()).Where(str => str != null).JoinString("-")).JoinString("; ");

        var serial = Bomb.GetSerialNumber();
        var curPos = 0;
        for (int i = 0; i < 6; i++)
        {
            var value = serial[i] >= '0' && serial[i] <= '9' ? serial[i] - '0' : serial[i] - 'A' + 1;
            curPos += value;
            curPos %= 24;
            braille[curPos / 6] ^= 1 << (curPos % 6);
            curPos++;
            flippedPositions.Add(curPos);
        }

        // Avoid use of the completely-empty cell
        if (braille.Any(b => b == 0))
            goto tryAgain;

        Debug.LogFormat("[Braille #{0}] {1}Braille patterns on module: {2}", _moduleId, first ? "" : "New ", Enumerable.Range(0, 4).Select(ix => Enumerable.Range(0, 6).Select(cell => (braille[ix] & (1 << cell)) == 0 ? null : (cell + 1).ToString()).Where(str => str != null).JoinString("-")).JoinString("; "));
        Debug.LogFormat("[Braille #{0}] {1}Braille patterns after flips: {2}", _moduleId, first ? "" : "New ", origPatterns);
        Debug.LogFormat("[Braille #{0}] {1} word: {2} ({3})", _moduleId, first ? "Solution" : "New solution", _word, _solutions[_word] + 1);
        if (first)
            Debug.LogFormat("[Braille #{0}] Flipped positions in order: {1}", _moduleId, flippedPositions.JoinString(", "));

        for (int i = 0; i < 4; i++)
        {
            var mesh = BrailleLetterMeshes.First(b => int.Parse(b.name.Substring("BrailleHighlight_".Length)) == braille[i]);
            BrailleLetterHighlights[i].mesh = mesh;
            var hClone = BrailleLetterHighlights[i].transform.Find("Highlight(Clone)");
            if (hClone != null)
                hClone.GetComponent<MeshFilter>().mesh = mesh;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Use !{0} cycle to show the Braille letters. Use !{0} press 1 to press the first letter, etc.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        int val;

        if (pieces.Length == 1 && pieces[0] == "cycle" && !_isSolved)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(.25f);
                var obj = BrailleLetterHighlights[i].gameObject;
                var hClone = obj.transform.Find("Highlight(Clone)");
                if (hClone != null)
                    obj = hClone.gameObject ?? obj;
                obj.SetActive(true);
                yield return new WaitForSeconds(1.25f);
                obj.SetActive(false);
                yield return new WaitForSeconds(.25f);
            }
        }
        else if (pieces.Length == 2 && pieces[0] == "press" && int.TryParse(pieces[1], out val) && val >= 1 && val <= 4 && !_isSolved)
        {
            yield return null;
            BrailleLetterSelectables[val - 1].OnInteract();
        }
    }
}
