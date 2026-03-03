using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Surfaces d'un patch de radiosité
    // ──────────────────────────────────────────────────────────────────────────
    public enum RadiosityPatchSurface
    {
        Floor, Ceiling, WallNorth, WallSouth, WallEast, WallWest
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Un carreau (patch) de surface
    // ──────────────────────────────────────────────────────────────────────────
    public class RadiosityPatch
    {
        /// <summary>Centre du patch en pieds Revit.</summary>
        public XYZ    Center      { get; set; }
        /// <summary>Normale unitaire pointant vers l'intérieur de la pièce.</summary>
        public XYZ    Normal      { get; set; }
        /// <summary>Premier axe tangent (dans le plan du patch).</summary>
        public XYZ    Tangent1    { get; set; }
        /// <summary>Second axe tangent (dans le plan du patch).</summary>
        public XYZ    Tangent2    { get; set; }
        /// <summary>Demi-largeur en pieds (axe Tangent1).</summary>
        public double HalfSizeU   { get; set; }
        /// <summary>Demi-hauteur en pieds (axe Tangent2).</summary>
        public double HalfSizeV   { get; set; }
        /// <summary>Aire en m².</summary>
        public double AreaM2      { get; set; }
        /// <summary>Réflectance diffuse (0–1).</summary>
        public double Reflectance { get; set; }
        /// <summary>Surface de la pièce à laquelle appartient le patch.</summary>
        public RadiosityPatchSurface Surface { get; set; }
        /// <summary>Éclairement direct reçu des luminaires (lux), rempli par LightingCalculator.</summary>
        public double DirectIlluminance { get; set; }
        /// <summary>Radiosité B = ρ × (E_direct + E_interréfléchi), en lux.</summary>
        public double Radiosity         { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Moteur de radiosité niveau 3 (Monte Carlo 64 rayons, Gauss-Seidel 6 iter)
    // ──────────────────────────────────────────────────────────────────────────
    public class RadiosityCalculator
    {
        private const int    MONTE_CARLO_RAYS     = 64;
        private const int    MAX_ITERATIONS        = 6;
        private const double CONVERGENCE_THRESHOLD = 0.01;   // 1 %
        private const double RAY_EPSILON           = 0.002;  // décalage anti auto-intersection (pieds)

        private readonly Random _rng;

        public RadiosityCalculator(int seed = 42)
        {
            _rng = new Random(seed);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  1. GÉNÉRATION DES PATCHS (6 surfaces de la BBox)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Génère les patchs de radiosité pour les 6 surfaces de la BBox.
        /// patchSizeMeters : taille cible des patchs en mètres (ex. 0.5).
        /// </summary>
        public List<RadiosityPatch> GenerateRoomPatches(
            BoundingBoxXYZ bbox,
            double patchSizeMeters,
            AnalysisSettings settings)
        {
            var patches = new List<RadiosityPatch>();
            double psf = patchSizeMeters / 0.3048; // mètres → pieds Revit

            double xMin = bbox.Min.X, xMax = bbox.Max.X;
            double yMin = bbox.Min.Y, yMax = bbox.Max.Y;
            double zMin = bbox.Min.Z, zMax = bbox.Max.Z;

            // SOL  (Z = Z_min, normale +Z vers le haut)
            AddSurfacePatches(patches,
                xMin, xMax, yMin, yMax,
                (cu, cv) => new XYZ(cu, cv, zMin),
                new XYZ(0, 0, 1), new XYZ(1, 0, 0), new XYZ(0, 1, 0),
                psf, settings.FloorReflectance, RadiosityPatchSurface.Floor);

            // PLAFOND  (Z = Z_max, normale -Z vers le bas)
            AddSurfacePatches(patches,
                xMin, xMax, yMin, yMax,
                (cu, cv) => new XYZ(cu, cv, zMax),
                new XYZ(0, 0, -1), new XYZ(1, 0, 0), new XYZ(0, 1, 0),
                psf, settings.CeilingReflectance, RadiosityPatchSurface.Ceiling);

            // MUR SUD  (Y = Y_min, normale +Y)
            AddSurfacePatches(patches,
                xMin, xMax, zMin, zMax,
                (cu, cv) => new XYZ(cu, yMin, cv),
                new XYZ(0, 1, 0), new XYZ(1, 0, 0), new XYZ(0, 0, 1),
                psf, settings.WallReflectance, RadiosityPatchSurface.WallSouth);

            // MUR NORD  (Y = Y_max, normale -Y)
            AddSurfacePatches(patches,
                xMin, xMax, zMin, zMax,
                (cu, cv) => new XYZ(cu, yMax, cv),
                new XYZ(0, -1, 0), new XYZ(1, 0, 0), new XYZ(0, 0, 1),
                psf, settings.WallReflectance, RadiosityPatchSurface.WallNorth);

            // MUR OUEST  (X = X_min, normale +X)
            AddSurfacePatches(patches,
                yMin, yMax, zMin, zMax,
                (cu, cv) => new XYZ(xMin, cu, cv),
                new XYZ(1, 0, 0), new XYZ(0, 1, 0), new XYZ(0, 0, 1),
                psf, settings.WallReflectance, RadiosityPatchSurface.WallWest);

            // MUR EST  (X = X_max, normale -X)
            AddSurfacePatches(patches,
                yMin, yMax, zMin, zMax,
                (cu, cv) => new XYZ(xMax, cu, cv),
                new XYZ(-1, 0, 0), new XYZ(0, 1, 0), new XYZ(0, 0, 1),
                psf, settings.WallReflectance, RadiosityPatchSurface.WallEast);

            return patches;
        }

        private delegate XYZ CenterBuilder(double cu, double cv);

        private void AddSurfacePatches(
            List<RadiosityPatch> patches,
            double uMin, double uMax,
            double vMin, double vMax,
            CenterBuilder buildCenter,
            XYZ normal, XYZ tangent1, XYZ tangent2,
            double patchSizeFeet,
            double reflectance,
            RadiosityPatchSurface surface)
        {
            for (double u = uMin; u < uMax; u += patchSizeFeet)
            {
                double actualU   = Math.Min(patchSizeFeet, uMax - u);
                double centerU   = u + actualU * 0.5;

                for (double v = vMin; v < vMax; v += patchSizeFeet)
                {
                    double actualV = Math.Min(patchSizeFeet, vMax - v);
                    double centerV = v + actualV * 0.5;

                    double areaM2 = (actualU * 0.3048) * (actualV * 0.3048);

                    patches.Add(new RadiosityPatch
                    {
                        Center      = buildCenter(centerU, centerV),
                        Normal      = normal,
                        Tangent1    = tangent1,
                        Tangent2    = tangent2,
                        HalfSizeU   = actualU * 0.5,
                        HalfSizeV   = actualV * 0.5,
                        AreaM2      = areaM2,
                        Reflectance = reflectance,
                        Surface     = surface
                    });
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  2. FACTEURS DE FORME  F[i,j]  (Monte Carlo, hémisphère cosinus-pondéré)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calcule la matrice N×N des facteurs de forme par échantillonnage Monte Carlo.
        /// F[i,j] = fraction du flux quittant le patch i qui arrive au patch j.
        /// </summary>
        public double[,] ComputeFormFactors(List<RadiosityPatch> patches)
        {
            int n = patches.Count;
            double[,] F = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                var src = patches[i];

                // Origine légèrement décalée pour éviter auto-intersection
                double ox = src.Center.X + src.Normal.X * RAY_EPSILON;
                double oy = src.Center.Y + src.Normal.Y * RAY_EPSILON;
                double oz = src.Center.Z + src.Normal.Z * RAY_EPSILON;

                for (int ray = 0; ray < MONTE_CARLO_RAYS; ray++)
                {
                    double dx, dy, dz;
                    SampleCosineHemisphere(src.Normal, src.Tangent1, src.Tangent2,
                                           out dx, out dy, out dz);

                    int hitIdx = FindFirstHit(ox, oy, oz, dx, dy, dz, patches, i);
                    if (hitIdx >= 0)
                        F[i, hitIdx] += 1.0 / MONTE_CARLO_RAYS;
                }
            }

            return F;
        }

        /// <summary>
        /// Génère une direction cosinus-pondérée dans l'hémisphère orienté par normal.
        /// Méthode de Malley (projection d'un point uniforme sur le disque unité).
        /// </summary>
        private void SampleCosineHemisphere(
            XYZ normal, XYZ t1, XYZ t2,
            out double dx, out double dy, out double dz)
        {
            double u1 = _rng.NextDouble();
            double u2 = _rng.NextDouble();

            double r   = Math.Sqrt(u1);
            double phi = 2.0 * Math.PI * u2;

            double lx = r * Math.Cos(phi);
            double ly = r * Math.Sin(phi);
            double lz = Math.Sqrt(Math.Max(0.0, 1.0 - u1));

            // Conversion en coordonnées monde
            dx = lx * t1.X + ly * t2.X + lz * normal.X;
            dy = lx * t1.Y + ly * t2.Y + lz * normal.Y;
            dz = lx * t1.Z + ly * t2.Z + lz * normal.Z;
        }

        /// <summary>
        /// Renvoie l'index du premier patch touché par le rayon (O + t×D, t>0),
        /// ou -1 si aucun hit.
        /// </summary>
        private static int FindFirstHit(
            double ox, double oy, double oz,
            double dx, double dy, double dz,
            List<RadiosityPatch> patches, int skipIndex)
        {
            double tMin  = double.MaxValue;
            int    hitIdx = -1;

            for (int j = 0; j < patches.Count; j++)
            {
                if (j == skipIndex) continue;

                var tgt   = patches[j];
                double nx = tgt.Normal.X, ny = tgt.Normal.Y, nz = tgt.Normal.Z;

                // Produit scalaire N · D
                double denom = nx * dx + ny * dy + nz * dz;
                if (Math.Abs(denom) < 1e-9) continue;  // rayon parallèle au plan

                // t = N · (C - O) / (N · D)
                double cx = tgt.Center.X - ox;
                double cy = tgt.Center.Y - oy;
                double cz = tgt.Center.Z - oz;
                double t  = (nx * cx + ny * cy + nz * cz) / denom;

                if (t <= RAY_EPSILON || t >= tMin) continue;

                // Point d'intersection
                double px = ox + dx * t;
                double py = oy + dy * t;
                double pz = oz + dz * t;

                // Coordonnées locales sur le patch (axes T1, T2)
                double pcx = px - tgt.Center.X;
                double pcy = py - tgt.Center.Y;
                double pcz = pz - tgt.Center.Z;

                double s = tgt.Tangent1.X * pcx + tgt.Tangent1.Y * pcy + tgt.Tangent1.Z * pcz;
                double q = tgt.Tangent2.X * pcx + tgt.Tangent2.Y * pcy + tgt.Tangent2.Z * pcz;

                if (Math.Abs(s) <= tgt.HalfSizeU && Math.Abs(q) <= tgt.HalfSizeV)
                {
                    tMin   = t;
                    hitIdx = j;
                }
            }

            return hitIdx;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  3. RÉSOLUTION GAUSS-SEIDEL
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Résout le système de radiosité itérativement (Gauss-Seidel).
        /// Chaque itération : B[i] = ρ[i] × (E_direct[i] + Σ_j F[i,j] × B[j])
        /// Convergence quand ΔB_max / B_max &lt; 1 %.
        /// </summary>
        public void SolveRadiosity(List<RadiosityPatch> patches, double[,] F)
        {
            int n = patches.Count;

            // Initialisation : B₀ = ρ × E_direct
            for (int i = 0; i < n; i++)
                patches[i].Radiosity = patches[i].Reflectance * patches[i].DirectIlluminance;

            for (int iter = 0; iter < MAX_ITERATIONS; iter++)
            {
                double maxChange = 0.0;

                for (int i = 0; i < n; i++)
                {
                    double irradiance = 0.0;
                    for (int j = 0; j < n; j++)
                        irradiance += F[i, j] * patches[j].Radiosity;

                    double newB = patches[i].Reflectance *
                                  (patches[i].DirectIlluminance + irradiance);

                    maxChange = Math.Max(maxChange, Math.Abs(newB - patches[i].Radiosity));
                    patches[i].Radiosity = newB;
                }

                double maxB = patches.Max(p => p.Radiosity);
                if (maxB > 1e-9 && maxChange / maxB < CONVERGENCE_THRESHOLD)
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  4. CONTRIBUTION INDIRECTE EN UN POINT DU PLAN DE TRAVAIL
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calcule l'éclairement indirect (lux) reçu en testPointFeet (pieds Revit)
        /// depuis tous les patchs de la liste (formule Lambertienne exacte).
        /// </summary>
        public static double GetIndirectIlluminanceAtPoint(
            XYZ testPointFeet, List<RadiosityPatch> patches)
        {
            double eIndirect = 0.0;

            foreach (var patch in patches)
            {
                // Vecteur de testPoint vers le centre du patch
                double ddx = patch.Center.X - testPointFeet.X;
                double ddy = patch.Center.Y - testPointFeet.Y;
                double ddz = patch.Center.Z - testPointFeet.Z;

                double rFeet = Math.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);
                if (rFeet < 0.01) continue;

                double inv = 1.0 / rFeet;
                double nx = ddx * inv, ny = ddy * inv, nz = ddz * inv; // direction normalisée

                // cos côté patch : angle entre la normale du patch et la direction VERS testPoint
                // dir vers testPoint = -n (direction opposée)
                double cosPatch = -(patch.Normal.X * nx + patch.Normal.Y * ny + patch.Normal.Z * nz);
                if (cosPatch <= 0) continue;

                // cos côté plan de travail horizontal (normale = +Z)
                double cosPoint = nz; // dot((0,0,1), direction_vers_patch) = nz
                if (cosPoint <= 0) continue;

                double rM = rFeet * 0.3048;
                double dE = (patch.Radiosity / Math.PI)
                           * cosPatch * cosPoint
                           * patch.AreaM2
                           / (rM * rM);

                eIndirect += dE;
            }

            return eIndirect;
        }
    }
}
