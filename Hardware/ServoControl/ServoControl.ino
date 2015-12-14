#include <Servo.h>

Servo servoHorizontal;  // create servo object to control a servo
//Servo servoVertical;  // create servo object to control a servo

int sensorPin = A0;    // select the input pin for the potentiometer
int sensorPin2 = A1;    // select the input pin for the potentiometer
int sensorValue = 0;  // variable to store the value coming from the sensor

void setup() {
  // Start the serial connection
  Serial.begin(57600);

  // Connect to the servos
  servoHorizontal.attach(9);
  //servoVertical.attach(10);
}

void loop() {

  // read horizontal joystick value
  sensorValue = analogRead(sensorPin);
  // convert the value to something the servo can use
  sensorValue = map(sensorValue, 0, 1023, 0, 180);
  servoHorizontal.write(sensorValue);

  Serial.print(sensorValue);
  Serial.print(" ");

  // read vertical joystick value
  sensorValue = analogRead(sensorPin2);
  sensorValue = map(sensorValue, 0, 1023, 0, 180);
  //servoVertical.write(sensorValue);
  Serial.println(sensorValue);

}
