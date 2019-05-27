using System;

namespace TestConsole
{
  class Program
  {
    static void Main(string[] args)
    {
      AudioFile.adjustSampleFormula = (sample, target, current) => sample * target / current;
      AudioFile.weighSampleFormula = ratio => 1d;
      WaveSound ws = new WaveSound(args[0]);
      ws.AdjustSamples(1.25 * ws.WeightedAverage, "test.wav");
      Console.ReadKey();
    }

    static void AllgemeineVerwendung(string QuellName, string ZielName)
    {
      //Erst die Formeln setzen:
      AudioFile.adjustSampleFormula = (sample, target, current) => sample * target / current;
      AudioFile.weighSampleFormula = ratio => 1d;
      //Soundobjekte erstellen:
      WaveSound ws = new WaveSound(QuellName);
      //Zielwert überprüfen:
      if(ws.WouldFit(0.5))
        //ggf. Zielwert umsetzen:
      ws.AdjustSamples(0.5, ZielName);
    }
  }
}