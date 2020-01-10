using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class customSongAnalyzer : MonoBehaviour
{
    // Start is called before the first frame update

    public AudioSource source;
    private AudioClip sound_file;
    public float bpm;
    private int sample_offset;
    private float sample_rate;
    private float average_ssa;
    private float sum_power;
    private float mean_power;
    private float sustainTime;
    private int num_samples;
    private int num_channels;
    private int window;
    private int idx;
    private int num_quiet_steps;
    private int activity_threshold;
    private bool found_silence;
    private int start_sample;
    private float[] samples;
    

    // this value should range somewhere between 10^(-96/20) and 10^(-32/20)
    // we will need to test this to find a good universal number
    private float power_threshold;
    
    // this value should be chosen for a bar (4 beats) but we can play with this
    public float sustainBeats;
    
    void Start()
    {
        // grab sound file variables
        sound_file = source.clip;
        sample_rate = sound_file.frequency;
        num_channels = sound_file.channels;
        num_samples = sound_file.samples;

        // calculate an good window size to test volume
        // based on RMS meters in studio one (150 ms)

        window = (int)(0.150 * sample_rate);

        // convert sustain number of beats to time and samples
        sustainTime =  60f * sustainBeats / bpm;
        activity_threshold = (int) Mathf.Ceil(sustainTime * sample_rate);

        // grab source samples from 2nd half of song
        sample_offset = (int)Mathf.Floor(0.5f * num_samples);
        samples = new float[(num_samples - sample_offset) * num_channels];
        sound_file.GetData(samples, sample_offset);
        
        // relabel the number of samples
        num_samples = samples.Length / num_channels;
        
        // first compute total average power for a baseline RMS level
        for (int i = 0; i < num_samples; i++)
        {
            sum_power += samples[i] * samples[i];
        }

        // Use the mean power to gauge an approximate power threshold
        mean_power = sum_power / num_samples;
        power_threshold = 0.01f * mean_power;

        // reset SSA for silence detection
        sum_power = 0;
        mean_power = 0;

        // some info
        Debug.Log("Sample Rate = " + sample_rate);
        Debug.Log("Number of samples = " + num_samples);
        Debug.Log("Sample Offset = " + sample_offset);
        Debug.Log("Number of Channels = " + num_channels);
        Debug.Log("Window Size, in samples = " + window);
        Debug.Log("Sustain Time in sec = " + sustainTime);
        Debug.Log("Activity Threshold in Samples = " + activity_threshold);
        Debug.Log("Start Analysis..........");

        // step through song calculating SSA
        for (int i = 0; i < Mathf.Floor(num_samples - window + 1); i++)
        {

            if (i == 0)
            {
                // calculate "volume" via SSA
                for (int j = 0; j < window * num_channels; j++)
                {
                    sum_power += samples[i * num_channels + j] * samples[i * num_channels + j];
                }

            }
            else
            {
                // remove uneeded contribution to SSM and add new contribution
                for (int j = 0; j < num_channels; j++)
                {
                    sum_power -= samples[(i - 1) * num_channels + j] * samples[(i - 1) * num_channels + j];
                    sum_power += samples[(i + window - 1) * num_channels + j] * samples[(i + window - 1) * num_channels + j];
                }
            }

            // calculate mean square magnitude, no square root saves CPU
            mean_power = sum_power / (window * num_channels);
            
            // check threshold and count how many times the check has happened
            if (mean_power < power_threshold)
            {
                num_quiet_steps += 1;
                
            }
            else
            {
                num_quiet_steps = 0;
            }

            // if "silence" has been achieved over the required sustain, print out the times
            if (num_quiet_steps > activity_threshold)
            {
                // output the time interval where the silence was detected
                float endingTime = (i + sample_offset) / sample_rate;
                float startingTime = endingTime - activity_threshold / sample_rate;
                found_silence = true;
                Debug.Log("Volume Threshold Reached for Full Sustain Time. The time interval is " + startingTime + " to " + endingTime + " sec.");

                // snap to nearest whole bar number and convert to the appropriate number of samples
                start_sample = Mathf.RoundToInt(Mathf.RoundToInt(startingTime * bpm / (4f * 60f)) * 4f * 60f * sample_rate / bpm);
                Debug.Log("Start Replay At " + start_sample + " samples.");

                break;
            }

        }

        if (!found_silence)
        {
            Debug.Log("No section of the song is under the RMS threshold for the required sustain time.");
        }

        Debug.Log("Analysis Finished.");

    }

}
