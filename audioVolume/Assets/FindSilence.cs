using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// class for "Silence" storage
public class Silence
{
    public float startTime { get; set; }
    public int threshold { get; set; }
}

public class FindSilence : MonoBehaviour
{
    public AudioClip sound_file;
    public float bpm;
    public float thresholdPercentage;
    public bool pollSilence;

    // Start is called before the first frame update

    int getRestartSample(AudioClip sound_file, float bpm)
    {
        // for any audio file, RMS = sqrt(SSA/num_points), volume = 20log10(RMS)
        // where SSA is the sum of squared amplitudes
        // This is expensive to compute so compare RMS^2 = SSA/num_points instead

        // hyper parameters (3) //

        // buffer time in sec to measure signal response
        // (default to 150 ms = 0.150 sec)
        float buffer = 0.150f;

        // latter fraction of the song to analyze
        // helps avoid soft starts (default to 0)
        float start_fraction = 0.0f;

        // % of overall sound levels to use as threshold
        // (default to 0.01, keep it from 0.01 to 0.075 for best results)
        float threshold_pct = thresholdPercentage;

        // ------------------------------------------- //

        // read soundfile data
        float sample_rate = sound_file.frequency;
        int num_channels = sound_file.channels;
        int true_num_samples = sound_file.samples;

        // calculate window size in samples
        int window = (int)(buffer * sample_rate);

        // grab source samples from 2nd half of song
        int sample_offset = (int)Mathf.Floor(start_fraction * true_num_samples);
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
        float mean_power = sum_power / num_samples;
        float power_threshold = threshold_pct * mean_power;

        // reset measures for "silence" detection
        sum_power = 0;
        mean_power = 0;

        // set up return objects
        var silences = new List<Silence>();
        int start_sample = 0;
        bool foundSilence = false;

        // some info
        Debug.Log("Sample Rate = " + sample_rate + " samples/sec.");
        Debug.Log("Number of samples = " + true_num_samples);
        Debug.Log("Sample Offset = " + sample_offset);
        Debug.Log("Number of Channels = " + num_channels);
        Debug.Log("Window Size, in samples = " + window);

        // the amount of quarter notes to judge "silence"
        // this number is dynamic to account for different mastering styles
        float[] sustain_beats_beat_list = new float[] {4, 3, 2, 1, 1f/2f, 1f/4f, 1f/8f, 1f/16f};

        foreach (float sustain_beats in sustain_beats_beat_list)
        {
            // convert sustain number of beats to time and samples
            float sustain_time =  60f * sustain_beats / bpm;
            int activity_threshold = (int) Mathf.Ceil(sustain_time * sample_rate);

            // set up counting variables
            int old_num_quiet_steps = activity_threshold;
            int num_quiet_steps = 0;

            // information about Sustain Time Constraint
            Debug.Log("Starting Analysis....");
            Debug.Log("Activity Threshold in Samples = " + activity_threshold);
            Debug.Log("Minimum sustain: " + sustain_time + " sec.");

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

                // if "silence" has been achieved over the required sustain, record data
                if (num_quiet_steps > old_num_quiet_steps)
                {
                    // add start time and threshold to "silence" recrd
                    int threshold = old_num_quiet_steps;
                    float startTime = (i + sample_offset - old_num_quiet_steps) / sample_rate;
                    silences.Add(new Silence() { startTime = startTime, threshold = threshold});

                    // update step threshold
                    old_num_quiet_steps = num_quiet_steps;

                }

            }

            // check if silences have been found
            if (silences.Count == 0)
            {
                Debug.Log("No section of the song is under the RMS threshold a sustain time of " + sustain_time + " sec.");
                Debug.Log("Trying again with smaller sustain constraints...");
            }
            else
            {
                if (pollSilence)
                {
                    Debug.Log("All Silences and Lengths");
                    for (int i = 0; i < silences.Count; i++)
                    {
                        if (i % 1000 == 0)
                        {
                            Debug.Log(silences[i].startTime + "  sec, " + silences[i].threshold / sample_rate + " sec sustain");
                        }
                    }
                }


                // "silences" are already sorted, remove early "silences" from list
                float max_time = silences[silences.Count - 1].startTime;
                silences.RemoveAll(s => (max_time - s.startTime) > sustain_time);

                // sort the remaining "silences" by sustain time
                silences = silences.OrderByDescending(s => s.threshold).ToList();

                // grab longest running "silence" step count
                int specific_step_threshold = silences[0].threshold;

                // calculate time intervals
                float start_time = silences[0].startTime;
                float end_time = start_time + specific_step_threshold / sample_rate;

                // snap times to nearest bar and then convert to nearest sample
                int start_bar = Mathf.RoundToInt(start_time * bpm / (4f * 60f));
                start_sample = Mathf.RoundToInt(start_bar * 4f * 60f * sample_rate / bpm);

                // Results
                Debug.Log("Sound level threshold reached. The time interval is " + start_time + " to " + end_time + " sec.");
                Debug.Log("Start replay at bar " + start_bar + " (" + start_sample + " samples or " + start_time + " sec)");

                // end search
                foundSilence = true;
                break;
            }

        }

        if (!foundSilence)
        {
            Debug.Log("Search has failed.");
        }
        
        Debug.Log("Analysis Finished.");

        return start_sample;
    }
    void Start()
    {   
        // how to use
        int s = getRestartSample(sound_file, bpm);
    }

}