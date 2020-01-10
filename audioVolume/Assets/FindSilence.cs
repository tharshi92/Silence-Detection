using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindSilence : MonoBehaviour
{
    public AudioClip sound_file;
    public float bpm;

    // Start is called before the first frame update

    int getRestartSample(AudioClip sound_file, float bpm)
    {
        // read soundfile data
        float sample_rate = sound_file.frequency;
        int num_channels = sound_file.channels;
        int true_num_samples = sound_file.samples;

        // calculate an good window size to test audio
        // based on RMS meters in studio one (150 ms)

        int window = (int)(0.150 * sample_rate);

        // convert sustain number of beats to time and samples
        // comes from testing, works best with a variey of music
        int sustainBeats = 3;
        float sustainTime =  60f * sustainBeats / bpm;
        int activity_threshold = (int) Mathf.Ceil(sustainTime * sample_rate);

        // grab source samples from 2nd half of song
        int sample_offset = (int)Mathf.Floor(0.5f * true_num_samples);
        float[] samples = new float[(true_num_samples - sample_offset) * num_channels];
        sound_file.GetData(samples, sample_offset);
        
        // relabel the number of samples
        int num_samples = samples.Length / num_channels;
        
        // first compute total average power for a baseline RMS level
        float sum_power = 0;
        for (int i = 0; i < num_samples; i++)
        {
            sum_power += samples[i] * samples[i];
        }

        // Use the mean power to gauge an approximate power threshold
        // I chose 1%, working with the most songs I tested
        float mean_power = sum_power / num_samples;
        float power_threshold = 0.01f * mean_power;

        // reset SSA for silence detection
        sum_power = 0;
        mean_power = 0;

        // set up counting variables
        int num_quiet_steps = 0;

        // some info
        //Debug.Log("Sample Rate = " + sample_rate + " samples/sec.");
        //Debug.Log("Number of samples = " + true_num_samples);
        //Debug.Log("Sample Offset = " + sample_offset);
        //Debug.Log("Number of Channels = " + num_channels);
        //Debug.Log("Window Size, in samples = " + window);
        Debug.Log("Required sustain in sec = " + sustainTime);
        //Debug.Log("Activity Threshold in Samples = " + activity_threshold);
        Debug.Log("Start Analysis..........");

        bool found_silence = false;
        int start_sample = 0;

        // RMS = sqrt(SSA/num_points), volume = 20log10(RMS)
        // this is expensive so compare RMS^2 = SSA/num_points intead

        // step through song calculating SSA
        for (int i = 0; i < Mathf.Floor(num_samples - window + 1); i++)
        {
            
            // Use rolling sum to keep an effcient record of measured power
            if (i == 0)
            {
                for (int j = 0; j < window * num_channels; j++)
                {
                    sum_power += samples[i * num_channels + j] * samples[i * num_channels + j];
                }

            }
            else
            {
                // remove uneeded contribution to SSA and add new contribution
                for (int j = 0; j < num_channels; j++)
                {
                    sum_power -= samples[(i - 1) * num_channels + j] * samples[(i - 1) * num_channels + j];
                    sum_power += samples[(i + window - 1) * num_channels + j] * samples[(i + window - 1) * num_channels + j];
                }
            }

            // calculate average power
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
                Debug.Log("Threshold Reached for Full Sustain Time. The time interval is " + startingTime + " to " + endingTime + " sec.");

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

        return start_sample;
    }
    void Start()
    {
        int s = getRestartSample(sound_file, bpm);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
