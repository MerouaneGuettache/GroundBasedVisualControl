#A Ground Based Visual Altitude Control of MAV

## Project Description
The following project showcase the implementation of an altitude controller of a micro air vehicule, where the feedback is assured by aground based camera.
It attempts to reproduce the same results as the experiment described in the following paper : http://eprints.uwe.ac.uk/31459/
We made the following changes on the method :
* the feedback is assured by a comparaison with the background, as described in the following tutorial :
 https://www.codeproject.com/Articles/1199224/Detecting-a-Drone-OpenCV-in-NET-for-Beginners-Emgu
* Instead of the cascade P, PI proposed by the paper, we implemented a simple PI controller.

The PSoC chip code to generate a voltage between 0-3.3V according to Serial command is given here as well. 