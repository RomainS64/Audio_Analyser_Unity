using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioAnalyser : MonoBehaviour
{
    [SerializeField] GameObject exempleGameObject;

    [SerializeField] private AudioSource source;
    [Tooltip("GetSpectrumData precision (must be a power of 2)")]
    [SerializeField] private int precision = 1024;
    [Tooltip("The greater the value, the fewer obstacles there will be")]
    [SerializeField] private float obstaclePrecission;
    [Tooltip("number of values ​​extracted per seconds")]
    [SerializeField] private int extractionNumber = 10;
    private List<Obstacle> obstacles;

    private float[] extractedFrequencies;
    private float[] spectrum;

    private AudioClip clip;

    private float step;
    private int clipPrecision;
    private bool extractReady = false;
    private bool isFirstTime = true;
    

    void Start()
    {
        obstacles = new List<Obstacle>();
        clip = source.clip;
        clipPrecision = (int)clip.length * extractionNumber;
        step = clip.length / clipPrecision;

        extractedFrequencies = new float[clipPrecision];
        spectrum = new float[precision];

        StartCoroutine(nameof(ExtractFrequencies));
    }

    private void Update()
    {
        //On attend que les frequences aient bien été récoltées
        if (!extractReady) return;
        
        if (isFirstTime)
        {
            //On créé les obstacles et lance le son
            CreateObstacles(obstaclePrecission);
            isFirstTime = false;
            source.volume = 1;
            source.time = 0;
            source.Play();
            return;
        }
        //On test si un obstacle doit apparaitre
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle.triggerTime <= source.time && !obstacle.isActivated)
            {
                //code lié aux obstacles
                //ICI on active/desactive un objet à chaques obstacles
                exempleGameObject.SetActive(!exempleGameObject.activeSelf);
                obstacle.isActivated = true;
            }
        }
    }
    IEnumerator ExtractFrequencies()
    {
        extractReady = false;
        yield return 0;
        for (int i = 0; i < clipPrecision; i++)
        {
            //On avance dans la musique
            source.time = step * i;
            yield return 0;
            Debug.Log("Generating AudioData " + (i+1)+"/"+clipPrecision);

            //On récupère le spectre 
            source.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

            //On ne garde que la fréquence la plus forte de l'extrait
            float frequency = GetHighestValueIndex(spectrum) * 
                ((AudioSettings.outputSampleRate / 2) / precision);
            extractedFrequencies[i] = frequency;
            
        }
        extractReady = true;
    }

    private void CreateObstacles(float margin = 0f)
    {
        //On détermine la variation minimale nécessaire pour ajouter un Objet
        float lim = GetAverageDelta(extractedFrequencies);
        lim += lim * margin;

        for (int i = 0; i < extractedFrequencies.Length - 1; i++)
        {
            //On calcule la variation(en HZ) entre la fréquences i et i+1
            float delta = Mathf.Abs(extractedFrequencies[i] - extractedFrequencies[i + 1]);
            if (delta > lim)
            {
                Debug.Log("New Obstacle !");
                //Si la variation est suffisante, on rajoute un obstacle
                obstacles.Add(new Obstacle(i * step + step / 2));
                //On saute une itération pour ne pas avoir deux obstacles simultanément(facultatif)
                i++;
            }
        }
    }

    private float GetAverageDelta(float[] array)
    {
        float[] deltaArray = new float[array.Length - 1];
        for(int i = 0; i < array.Length - 1; i++)
        {
            deltaArray[i] = Mathf.Abs(array[i] - array[i + 1]);
        }
        float avg = 0;
        foreach(float delta in deltaArray)avg += delta;
        return avg / deltaArray.Length;
    }
    private int GetHighestValueIndex(float[] values)
    {
        float max = float.NegativeInfinity;
        int i = 0;
        int index=0;
        foreach(float value in values)
        {
            if (value > max)
            {
                max = value;
                index = i;
            }
            i++;
        }
        return index;
    }

    
    

}

public class Obstacle
{
    public float triggerTime;
    public bool isActivated = false;
    public Obstacle(float triggerTime)
    {
        this.triggerTime = triggerTime;
    }
}