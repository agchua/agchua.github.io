# Wireframes

Design explorations for the personal site. Every file is a self-contained HTML page (no build step, no
dependencies). Open `index.html` in a browser to browse them all.

Style references: lakewyatt.com (primary: plain monospace, single narrow column) and lucapicazo.com (secondary).

Each page has blue wireframe notes; the **hide notes** button (bottom-right) shows the page without them.
All animations respect `prefers-reduced-motion` (they render a still frame instead).

## Options

| | File | Layout | Graphic |
|---|---|---|---|
| A | `option-a.html` | single column | elementary cellular automaton strip (rule 30/90/110/184/45) as the header rule; text "decodes" on load |
| B | `option-b.html` | text left, sticky figure right | de Jong attractor, parameters drift; click morphs to a new preset |
| C | `option-c.html` | single column | Julia set rendered in ASCII characters; cursor scrubs c |
| D | `option-d.html` | text on a white sheet | full-page pendulum phase portrait in the background; click toggles friction |
| E | `option-e.html` | single column | Cahn–Hilliard spinodal decomposition styled as a micrograph; click re-quenches |
| F | `option-f.html` | text left, figure right | 2D Lennard-Jones molecular dynamics with a temperature slider |
| G | `option-g.html` | text left, figure right | phase-field (Kobayashi) close-up of dendrites at a Czochralski growth front |
| H | `option-h.html` | resume content in a white box, no wireframe notes | full-page background of large silicon grains (viewed down ⟨111⟩); each new grain grows over older ones |

## Notes on the simulations

- **E: Cahn–Hilliard**: ∂c/∂t = ∇²(c³ − c − γ∇²c), explicit Euler on a 240×80 periodic grid.
- **F: Lennard-Jones MD**: 196 atoms, velocity Verlet, Berendsen thermostat; atoms shaded by neighbour
  count (6 = crystal, 5/7 = defect, other = liquid). Melts around T* ≈ 0.7.
- **G and H: Kobayashi (1993) phase-field model**: phase p and temperature T, with anisotropic interface
  energy ε(θ) = ε̄(1 + δ cos j(θ − θ₀)) and latent heat K released at the front.
  - G: 4-fold (cubic) anisotropy; the crystal sits on top and the view scrolls as the tips advance (the "pull").
  - H: 6-fold anisotropy and a random orientation per grain. Each grain grows by itself on its own 176×176
    grid and is then painted into the background, so new grains layer over old ones and fully covered
    grains simply disappear. A new nucleus appears every 3 s or on click; nothing remelts.
