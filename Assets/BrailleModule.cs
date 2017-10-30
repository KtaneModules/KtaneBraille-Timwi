using System;
using System.Collections.Generic;
using System.Linq;
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

    private static int _moduleIdCounter = 1;
    private int _moduleId;

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
crying=4,headed=2,master=2,served=2,winter=3".Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Split('=')).ToDictionary(arr => arr[0], arr => int.Parse(arr[1]));

    private string _word;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        var words = _solutions.Keys.ToArray();
        _word = words[Rnd.Range(0, words.Length)];
    }
}
