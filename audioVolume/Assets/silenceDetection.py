# -*- coding: utf-8 -*-
"""
Spyder Editor

This is a temporary script file.
"""

from scipy.io import wavfile
import numpy as np
import matplotlib.pyplot as plt

sample_rate, sound_data = wavfile.read('short_audio.wav')
sound_data = sound_data / 2.**15
n_samples = np.shape(sound_data)[0]
n_channels = np.shape(sound_data)[1]

window = int(0.3 * sample_rate)
bpm = 126
rms_threshold = 0.006
sustain_beats = 4

powers = np.sum(sound_data**2, axis=1)
test_length = 10
test_window = 3

print(powers[:test_length])

ssa_eff = 0
for i in range(test_length - test_window + 1):
    if i == 0:
        for j in range(test_window):
            ssa_eff += powers[i + j]
    else:
        old = powers[i - 1]
        new = powers[i + test_window]
        #print("\nold = p[{}] = {} and new = p[{}] = {}".format(i - 1, old, i + test_window - 1, new))
        ssa_eff -= powers[i - 1]
        ssa_eff += powers[i + test_window - 1]
        
    print("ssa = {}".format(ssa_eff))
        
print('\n\nOld Calculation............')

ssa = 0
for i in range(test_length - test_window + 1):
    for j in range(test_window):
        ssa += powers[i + j]
        
        
    print("ssa = {}".format(ssa))
    ssa = 0